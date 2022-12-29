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
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IEnumerable<IAElfLogEventProcessor<TransactionInfo>> processors, ILogger<TransactionDataHandler> logger) : base(clusterClient, objectMapper,
        aelfIndexerClientInfoProvider, logger)
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
        try
        {
            await ProcessTransactionsAsync(data);
            await ProcessLogEventsAsync(data);
        }
        catch (Exception e)
        {
            Logger.LogError(e, e.Message);
        }
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