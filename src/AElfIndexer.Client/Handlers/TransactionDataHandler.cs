using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public abstract class TransactionDataHandler<T> : BlockChainDataHandler<TransactionInfo,T>
{
    private readonly IEnumerable<IAElfLogEventProcessor<T>> _processors;

    protected TransactionDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider<T> aelfIndexerClientInfoProvider,
        IEnumerable<IAElfLogEventProcessor<T>> processors) : base(clusterClient, objectMapper,
        aelfIndexerClientInfoProvider)
    {
        _processors = processors;
    }

    public override BlockFilterType FilterType => BlockFilterType.Transaction;

    protected override List<TransactionInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return ObjectMapper.Map<List<TransactionDto>, List<TransactionInfo>>(blockDto.Transactions);
    }

    protected override async Task ProcessDataAsync(List<TransactionInfo> data)
    {
        await ProcessTransactionsAsync(data);
        await ProcessLogEventsAsync(data);
    }

    protected abstract Task ProcessTransactionsAsync(List<TransactionInfo> transactions);
    private async Task ProcessLogEventsAsync(List<TransactionInfo> transactions)
    {
        if (!_processors.Any()) return;
        foreach (var transaction in transactions)
        {
            foreach (var logEvent in transaction.LogEvents)
            {
                var processor = _processors.FirstOrDefault(p =>
                    p.GetContractAddress() == logEvent.ContractAddress && p.GetEventName() == logEvent.EventName);
                if (processor == null) continue;
                await processor.HandleEventAsync(logEvent,
                    ObjectMapper.Map<TransactionInfo, LogEventContext>(transaction));
            }
        }
    }
}