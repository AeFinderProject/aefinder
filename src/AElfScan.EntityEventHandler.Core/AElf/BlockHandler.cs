using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Index = System.Index;

namespace AElfScan.AElf;

public class BlockHandler:IDistributedEventHandler<NewBlockEto>,
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

    public async Task HandleEventAsync(NewBlockEto eventData)
    {
        _logger.LogInformation($"block is adding, id: {eventData.BlockHash}  , BlockNumber: {eventData.BlockNumber} , IsConfirmed: {eventData.IsConfirmed}");
        var blockIndex = await _blockIndexRepository.GetAsync(q=>
            q.Term(i=>i.Field(f=>f.BlockHash).Value(eventData.BlockHash)));
        if (blockIndex != null)
        {
            _logger.LogInformation($"block already exist-{blockIndex.Id}, Add failure!");
        }
        else
        {
            await _blockIndexRepository.AddAsync(eventData);
            _ = Task.Run(async () => { await _blockIndexHandler.ProcessNewBlockAsync(eventData); });

            List<TransactionIndex> transactionIndexList = new List<TransactionIndex>();
            List<LogEventIndex> logEventIndexList = new List<LogEventIndex>();
            foreach (var transaction in eventData.Transactions)
            {
                var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
                transactionIndex.Id = GetTransactionIndexId(eventData.BlockHash, transaction.TransactionId);
                // await _transactionIndexRepository.AddOrUpdateAsync(transactionIndex);
                transactionIndexList.Add(transactionIndex);

                foreach (var logEvent in transaction.LogEvents)
                {
                    var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                    logEventIndex.Id =
                        GetLogEventIndexId(eventData.BlockHash, transaction.TransactionId, logEvent.Index);
                    // await _logEventIndexRepository.AddOrUpdateAsync(logEventIndex);
                    logEventIndexList.Add(logEventIndex);
                }
            }

            if (transactionIndexList.Count > 0)
            {
                await _transactionIndexRepository.BulkAddOrUpdateAsync(transactionIndexList);
            }
            if (logEventIndexList.Count > 0)
            {
                await _logEventIndexRepository.BulkAddOrUpdateAsync(logEventIndexList);
            }
        }
        

        
    }

    public async Task HandleEventAsync(ConfirmBlocksEto eventData)
    {
        List<BlockIndex> confirmBlockIndexList = new List<BlockIndex>();
        List<TransactionIndex> confirmTransactionIndexList = new List<TransactionIndex>();
        List<LogEventIndex> confirmLogEventIndexList = new List<LogEventIndex>(); 
        var indexes = new List<BlockIndex>();
        foreach (var confirmBlock in eventData.ConfirmBlocks)
        {
            _logger.LogInformation($"block:{confirmBlock.BlockNumber} is confirming");
            var blockIndex = _objectMapper.Map<ConfirmBlockEto, BlockIndex>(confirmBlock);
            blockIndex.IsConfirmed = true;
            foreach (var transaction in blockIndex.Transactions)
            {
                transaction.IsConfirmed = true;
            }

            confirmBlockIndexList.Add(blockIndex);
            // await _blockIndexRepository.UpdateAsync(blockIndex);
            indexes.Add(blockIndex);

            foreach (var transaction in confirmBlock.Transactions)
            {
                var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
                transactionIndex.Id = GetTransactionIndexId(confirmBlock.BlockHash, transaction.TransactionId);
                transactionIndex.IsConfirmed = true;
                foreach (var logEvent in transactionIndex.LogEvents)
                {
                    logEvent.IsConfirmed = true;
                }
                // await _transactionIndexRepository.UpdateAsync(transactionIndex);
                confirmTransactionIndexList.Add(transactionIndex);

                foreach (var logEvent in transaction.LogEvents)
                {
                    var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                    logEventIndex.Id =
                        GetLogEventIndexId(confirmBlock.BlockHash, transaction.TransactionId, logEvent.Index);
                    logEventIndex.IsConfirmed = true;
                    // await _logEventIndexRepository.UpdateAsync(logEventIndex);
                    confirmLogEventIndexList.Add(logEventIndex);
                }
            }

            //find the same height blocks
            var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.BlockNumber).Value(confirmBlock.BlockNumber)));
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
                // await _blockIndexRepository.DeleteAsync(forkBlock);
                // _logger.LogInformation($"block {forkBlock.BlockHash} has been deleted.");
                foreach (var transaction in forkBlock.Transactions)
                {
                    var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
                    transactionIndex.Id = GetTransactionIndexId(confirmBlock.BlockHash,transaction.TransactionId);
                    forkTransactionIndexList.Add(transactionIndex);
                    
                    foreach (var logEvent in transaction.LogEvents)
                    {
                        var logEventIndex = _objectMapper.Map<LogEvent, LogEventIndex>(logEvent);
                        logEventIndex.Id = GetLogEventIndexId(confirmBlock.BlockHash, transaction.TransactionId,
                            logEvent.Index);
                        confirmLogEventIndexList.Add(logEventIndex);
                    }
                }
            }

            if (forkBlockIndexList.Count > 0)
            {
                await _blockIndexRepository.BulkDelete(forkBlockIndexList);
            }
            if (forkTransactionIndexList.Count > 0)
            {
                await _transactionIndexRepository.BulkDelete(forkTransactionIndexList);
            }
            if (forkLogEventIndexList.Count > 0)
            {
                await _logEventIndexRepository.BulkDelete(forkLogEventIndexList);
            }
        }

        await _blockIndexRepository.BulkAddOrUpdateAsync(confirmBlockIndexList);
        if (confirmTransactionIndexList.Count > 0)
        {
            await _transactionIndexRepository.BulkAddOrUpdateAsync(confirmTransactionIndexList);
        }
        if (confirmLogEventIndexList.Count > 0)
        {
            await _logEventIndexRepository.BulkAddOrUpdateAsync(confirmLogEventIndexList);
        }
        
        _ = Task.Run(async () => { await _blockIndexHandler.ProcessConfirmBlocksAsync(indexes); });
    }

    private string GetTransactionIndexId(string blockHash,string transactionId)
    {
        return blockHash + "_" + transactionId;
    }

    private string GetLogEventIndexId(string blockHash, string transactionId, int index)
    {
        return blockHash + "_" + transactionId + "_" + index;
    }
    
}