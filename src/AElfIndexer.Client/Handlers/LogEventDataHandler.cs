using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public class LogEventDataHandler : BlockChainDataHandler<LogEventInfo>
{
    private readonly IEnumerable<IAElfLogEventProcessor> _processors;

    public LogEventDataHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IEnumerable<IAElfLogEventProcessor> processors, ILogger<LogEventDataHandler> logger) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider, logger)
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