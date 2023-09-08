using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class TransactionDataHandler : BlockChainDataHandler<TransactionInfo>
{
    private readonly IEnumerable<IAElfLogEventProcessor<TransactionInfo>> _processors;

    protected TransactionDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<TransactionInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors, ILogger<TransactionDataHandler> logger)
        : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider, logger, dAppDataProvider,
            blockStateSetProvider, dAppDataIndexManagerProvider)
    {
        _processors = processors;
    }

    public override BlockFilterType FilterType => BlockFilterType.Transaction;

    protected override List<TransactionInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return ObjectMapper.Map<List<TransactionDto>, List<TransactionInfo>>(blockDto.Transactions);
    }

    protected override async Task ProcessDataAsync(string chainId, List<TransactionInfo> data)
    {
        foreach (var transactionInfo in data)
        {
            try
            {
                await ProcessTransactionAsync(transactionInfo);
            }
            catch (Exception e)
            {
                throw new DAppHandlingException(
                    $"Handle Transaction Error! ChainId: {transactionInfo.ChainId} BlockHeight: {transactionInfo.BlockHeight} BlockHash: {transactionInfo.BlockHash} TransactionId: {transactionInfo.TransactionId}.",
                    e);
            }
        }
        
        await ProcessLogEventsAsync(data);
    }

    protected abstract Task ProcessTransactionAsync(TransactionInfo transaction);
    private async Task ProcessLogEventsAsync(List<TransactionInfo> transactions)
    {
        if (!_processors.Any()) return;
        foreach (var transaction in transactions)
        {
            foreach (var logEvent in transaction.LogEvents)
            {
                var processor = _processors.FirstOrDefault(p =>
                    p.GetContractAddress(logEvent.ChainId) == logEvent.ContractAddress && p.GetEventName() == logEvent.EventName);
                if (processor == null) continue;
                await processor.HandleEventAsync(logEvent,
                    ObjectMapper.Map<TransactionInfo, LogEventContext>(transaction));
            }
        }
    }
}