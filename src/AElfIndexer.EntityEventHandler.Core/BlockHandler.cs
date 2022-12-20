using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Index = System.Index;

namespace AElfIndexer.AElf;

public class BlockHandler:IDistributedEventHandler<NewBlocksEto>,
    IDistributedEventHandler<ConfirmBlocksEto>,
    ITransientDependency
{
    private readonly INESTRepository<BlockIndex, string> _blockIndexRepository;
    private readonly ILogger<BlockHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly INESTRepository<LogEventIndex, string> _logEventIndexRepository;
    private readonly IBlockIndexHandler _blockIndexHandler;

    public BlockHandler(
        INESTRepository<BlockIndex,string> blockIndexRepository,
        INESTRepository<TransactionIndex,string> transactionIndexRepository,
        INESTRepository<LogEventIndex,string> logEventIndexRepository,
        ILogger<BlockHandler> logger,
        IObjectMapper objectMapper, IBlockIndexHandler blockIndexHandler)
    {
        _blockIndexRepository = blockIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _transactionIndexRepository = transactionIndexRepository;
        _logEventIndexRepository = logEventIndexRepository;
        _blockIndexHandler = blockIndexHandler;
    }

    public async Task HandleEventAsync(NewBlocksEto eventData)
    {
        _logger.LogInformation(
            $"blocks is adding, start BlockNumber: {eventData.NewBlocks.First().BlockHeight} , IsConfirmed: {eventData.NewBlocks.First().Confirmed}, end BlockNumber: {eventData.NewBlocks.Last().BlockHeight}");

        try
        {
            var blockIndexList = new List<BlockIndex>();
            foreach (var newBlock in eventData.NewBlocks)
            {
                var blockIndex = _objectMapper.Map<NewBlockEto, BlockIndex>(newBlock);
                blockIndex.TransactionIds = newBlock.Transactions.Select(b => b.TransactionId).ToList();
                blockIndex.LogEventCount = newBlock.Transactions.Sum(t => t.LogEvents.Count);
                blockIndexList.Add(blockIndex);
            }

            // var blockIndexList = _objectMapper.Map<List<NewBlockEto>, List<BlockIndex>>(eventData.NewBlocks);
            await _blockIndexRepository.BulkAddOrUpdateAsync(blockIndexList);

            List<TransactionIndex> transactionIndexList = new List<TransactionIndex>();
            List<LogEventIndex> logEventIndexList = new List<LogEventIndex>();

            foreach (var newBlock in eventData.NewBlocks)
            {
                foreach (var transaction in newBlock.Transactions)
                {
                    var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
                    transactionIndexList.Add(transactionIndex);

                    foreach (var logEvent in transaction.LogEvents)
                    {
                        var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                        logEventIndexList.Add(logEventIndex);
                    }
                }
            }

            if (transactionIndexList.Count > 0)
            {
                _logger.LogDebug(
                    $"Transaction is bulk-adding, its start block number:{eventData.NewBlocks.First().BlockHeight}, total transaction count:{transactionIndexList.Count}");
                await _transactionIndexRepository.BulkAddOrUpdateAsync(transactionIndexList);
            }

            if (logEventIndexList.Count > 0)
            {
                _logger.LogDebug(
                    $"LogEvent is bulk-adding, its start block number:{eventData.NewBlocks.First().BlockHeight}, total logevent count:{logEventIndexList.Count}");
                await _logEventIndexRepository.BulkAddOrUpdateAsync(logEventIndexList);
            }
            
            var blockDtos = _objectMapper.Map<List<NewBlockEto>, List<BlockWithTransactionDto>>(eventData.NewBlocks);
            foreach (var dto in blockDtos)
            {
                _ = Task.Run(async () => { await _blockIndexHandler.ProcessNewBlockAsync(dto); });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Handle newBlocks add event error:{e.Message}，start BlockHeight: {eventData.NewBlocks.First().BlockHeight}, end BlockHeight: {eventData.NewBlocks.Last().BlockHeight}");
            HandleEventAsync(eventData);
        }
    }

    public async Task HandleEventAsync(ConfirmBlocksEto eventData)
    {
        List<BlockIndex> confirmBlockIndexList = new List<BlockIndex>();
        List<TransactionIndex> confirmTransactionIndexList = new List<TransactionIndex>();
        List<LogEventIndex> confirmLogEventIndexList = new List<LogEventIndex>(); 
        var indexes = new List<BlockIndex>();
        _logger.LogInformation($"block:{eventData.ConfirmBlocks.First().BlockHeight}-{eventData.ConfirmBlocks.Last().BlockHeight} is confirming");
        foreach (var confirmBlock in eventData.ConfirmBlocks)
        {
            var blockIndex = _objectMapper.Map<ConfirmBlockEto, BlockIndex>(confirmBlock);
            blockIndex.TransactionIds = confirmBlock.Transactions.Select(b => b.TransactionId).ToList();
            
            // _logger.LogInformation("confirm block transacionids count:" + blockIndex.TransactionIds.Count);
            blockIndex.LogEventCount = confirmBlock.Transactions.Sum(t => t.LogEvents.Count);
            blockIndex.Confirmed = true;

            confirmBlockIndexList.Add(blockIndex);
            indexes.Add(blockIndex);

            foreach (var transaction in confirmBlock.Transactions)
            {
                var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
                transactionIndex.Confirmed = true;
                foreach (var logEvent in transactionIndex.LogEvents)
                {
                    logEvent.Confirmed = true;
                }
                confirmTransactionIndexList.Add(transactionIndex);

                foreach (var logEvent in transaction.LogEvents)
                {
                    var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                    logEventIndex.Confirmed = true;
                    confirmLogEventIndexList.Add(logEventIndex);
                }
            }

            //find the same height blocks
            var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(confirmBlock.ChainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.BlockHeight).Value(confirmBlock.BlockHeight)));
            QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Bool(b => b.Must(mustQuery));

            var forkBlockList = await _blockIndexRepository.GetListAsync(Filter);
            if (forkBlockList.Item1 == 0)
            {
                continue;
            }

            //delete the same height fork block
            List<BlockIndex> forkBlockIndexList = new List<BlockIndex>();
            List<TransactionIndex> forkTransactionIndexList = new List<TransactionIndex>();
            List<LogEventIndex> forkLogEventIndexList = new List<LogEventIndex>();
            foreach (var forkBlock in forkBlockList.Item2)
            {
                if (forkBlock.BlockHash == confirmBlock.BlockHash)
                {
                    continue;
                }

                forkBlockIndexList.Add(forkBlock);

                var transactionIndexList = await GetTransactionListAsync(forkBlock.ChainId,forkBlock.BlockHash);
                forkTransactionIndexList.AddRange(transactionIndexList);

                foreach (var transaction in transactionIndexList)
                {
                    foreach (var logEvent in transaction.LogEvents)
                    {
                        var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                        forkLogEventIndexList.Add(logEventIndex);
                    }
                }
            }

            if (forkBlockIndexList.Count > 0)
            {
                _logger.LogDebug($"bulk delete blocks,total count {forkBlockIndexList.Count}");
                await _blockIndexRepository.BulkDeleteAsync(forkBlockIndexList);
            }
            if (forkTransactionIndexList.Count > 0)
            {
                _logger.LogDebug($"bulk delete transactions,total count {forkTransactionIndexList.Count}");
                await _transactionIndexRepository.BulkDeleteAsync(forkTransactionIndexList);
            }
            if (forkLogEventIndexList.Count > 0)
            {
                _logger.LogDebug($"bulk delete log events,total count {forkLogEventIndexList.Count}");
                await _logEventIndexRepository.BulkDeleteAsync(forkLogEventIndexList);
            }
        }

        _logger.LogDebug($"blocks is confirming,start {confirmBlockIndexList.First().BlockHeight} end {confirmBlockIndexList.Last().BlockHeight},total confirm {confirmBlockIndexList.Count}");
        await _blockIndexRepository.BulkAddOrUpdateAsync(confirmBlockIndexList);
        if (confirmTransactionIndexList.Count > 0)
        {
            _logger.LogDebug($"transactions is confirming,start {confirmTransactionIndexList.First().BlockHeight} end {confirmTransactionIndexList.Last().BlockHeight},total confirm {confirmTransactionIndexList.Count}");
            await _transactionIndexRepository.BulkAddOrUpdateAsync(confirmTransactionIndexList);
        }
        if (confirmLogEventIndexList.Count > 0)
        {
            _logger.LogDebug($"log events is confirming,start {confirmLogEventIndexList.First().BlockHeight} end {confirmLogEventIndexList.Last().BlockHeight},total confirm {confirmLogEventIndexList.Count}");
            await _logEventIndexRepository.BulkAddOrUpdateAsync(confirmLogEventIndexList);
        }
        
        var blockDtos = _objectMapper.Map<List<ConfirmBlockEto>, List<BlockWithTransactionDto>>(eventData.ConfirmBlocks);
        _ = Task.Run(async () => { await _blockIndexHandler.ProcessConfirmedBlocksAsync(blockDtos); });
    }

    private async Task<List<TransactionIndex>> GetTransactionListAsync(string chainId,string blockHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TransactionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.BlockHash).Value(blockHash)));
        QueryContainer Filter(QueryContainerDescriptor<TransactionIndex> f) => f.Bool(b => b.Must(mustQuery));

        var forkTransactionList = await _transactionIndexRepository.GetListAsync(Filter);
        if (forkTransactionList.Item1 == 0)
        {
            return new List<TransactionIndex>();
        }

        return forkTransactionList.Item2;
    }


}