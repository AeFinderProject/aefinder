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
    IDistributedEventHandler<ConfirmTransactionsEto>,
    IDistributedEventHandler<ConfirmLogEventsEto>,ITransientDependency
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
        // var blockIndex = await _blockIndexRepository.GetAsync(q=>
        //     q.Term(i=>i.Field(f=>f.BlockHash).Value(eventData.BlockHash)));
        // if (blockIndex != null)
        // {
        //     _logger.LogInformation($"block already exist-{blockIndex.Id}, Add failure!");
        // }
        // else
        // {
        //     await _blockIndexRepository.AddAsync(eventData);
        //     _ = Task.Run(async () => { await _blockIndexHandler.ProcessNewBlockAsync(eventData); });
        // }
        await _blockIndexRepository.AddOrUpdateAsync(eventData);
        _ = Task.Run(async () => { await _blockIndexHandler.ProcessNewBlockAsync(eventData); });

        foreach (var transaction in eventData.Transactions)
        {
            var transactionIndex = _objectMapper.Map<Transaction, TransactionIndex>(transaction);
            transactionIndex.Id = transaction.BlockHash;
            await _transactionIndexRepository.AddOrUpdateAsync(transactionIndex);
        }

        
    }
    
    // public async Task HandleEventAsync(NewTransactionEto eventData)
    // {
    //     _logger.LogInformation($"transaction is adding, id: {eventData.TransactionId}  , BlockNumber: {eventData.BlockNumber} , IsConfirmed: {eventData.IsConfirmed}");
    //     var transactionIndex = await _transactionIndexRepository.GetAsync(q=>
    //         q.Term(i=>i.Field(f=>f.Id).Value(eventData.TransactionId)));
    //     if (transactionIndex != null)
    //     {
    //         _logger.LogInformation($"transaction already exist-{transactionIndex.Id}, Add failure!");
    //     }
    //     else
    //     {
    //         await _transactionIndexRepository.AddAsync(eventData);
    //     }
    //     
    // }
    
    // public async Task HandleEventAsync(NewLogEventEto eventData)
    // {
    //     _logger.LogInformation($"logevent is adding, id: {eventData.Id}  , BlockNumber: {eventData.BlockNumber} , TransactionId: {eventData.TransactionId} , EventName: {eventData.EventName}");
    //     var logEventIndex = await _logEventIndexRepository.GetAsync(q=>
    //         q.Term(i=>i.Field(f=>f.Id).Value(eventData.Id)));
    //     if (logEventIndex != null)
    //     {
    //         _logger.LogInformation($"logevent already exist-{logEventIndex.Id}, Add failure!");
    //     }
    //     else
    //     {
    //         await _logEventIndexRepository.AddAsync(eventData);
    //     }
    //     
    // }

    public async Task HandleEventAsync(ConfirmBlocksEto eventData)
    {
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

            await _blockIndexRepository.UpdateAsync(blockIndex);
            indexes.Add(blockIndex);

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
            foreach (var forkBlock in forkBlockList.Item2)
            {
                if (forkBlock.BlockHash == confirmBlock.BlockHash)
                {
                    continue;
                }

                await _blockIndexRepository.DeleteAsync(forkBlock);
                _logger.LogInformation($"block {forkBlock.BlockHash} has been deleted.");
            }
        }

        _ = Task.Run(async () => { await _blockIndexHandler.ProcessConfirmBlocksAsync(indexes); });
    }
    
    public async Task HandleEventAsync(ConfirmTransactionsEto eventData)
    {
        foreach (var confirmTransaction in eventData.ConfirmTransactions)
        {
            _logger.LogInformation($"transaction:{confirmTransaction.Id} is confirming");
            var transactionIndex = _objectMapper.Map<ConfirmTransactionEto, TransactionIndex>(confirmTransaction);
            transactionIndex.IsConfirmed = true;
            foreach (var logEvent in transactionIndex.LogEvents)
            {
                logEvent.IsConfirmed = true;
            }

            await _transactionIndexRepository.UpdateAsync(transactionIndex);
        }

    }
    
    public async Task HandleEventAsync(ConfirmLogEventsEto eventData)
    {
        foreach (var confirmLogEventEto in eventData.ConfirmLogEvents)
        {
            _logger.LogInformation($"logevent:{confirmLogEventEto.Id} is confirming");
            var logEventIndex = _objectMapper.Map<ConfirmLogEventEto, LogEventIndex>(confirmLogEventEto);
            logEventIndex.IsConfirmed = true;

            await _logEventIndexRepository.UpdateAsync(logEventIndex);
        }

    }
}