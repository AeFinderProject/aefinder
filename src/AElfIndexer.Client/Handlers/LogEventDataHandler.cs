using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public class LogEventDataHandler<T> : BlockChainDataHandler<LogEventInfo,T>
{
    private readonly IEnumerable<IAElfLogEventProcessor<T>> _processors;

    public LogEventDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IEnumerable<IAElfLogEventProcessor<T>> processors) : base(clusterClient, objectMapper)
    {
        _processors = processors;
    }

    public override BlockFilterType FilterType => BlockFilterType.LogEvent;

    protected override List<LogEventInfo> GetData(BlockWithTransactionDto blockDto)
    {
        return ObjectMapper.Map<List<LogEventDto>, List<LogEventInfo>>(blockDto.Transactions.SelectMany(t => t.LogEvents).ToList());
    }

    protected override async Task ProcessDataAsync(List<LogEventInfo> data)
    {
        foreach (var logEvent in data)
        {
            var processor = _processors.FirstOrDefault(p =>
                p.GetContractAddress() == logEvent.ContractAddress && p.GetEventName() == logEvent.EventName);
            if (processor == null) continue;
            await processor.HandleEventAsync(logEvent);
        }
    }
}