using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
using AElfScan.EventData;
using AElfScan.Grain;
using AElfScan.Orleans;
using Microsoft.Extensions.Logging;

namespace AElfScan.AElf;

public class AElfTestAppService:AElfScanAppService,IAElfAppService
{
    private readonly IOrleansClusterClientFactory _clusterClientFactory;
    private readonly ILogger<AElfTestAppService> _logger;

    public AElfTestAppService(
        IOrleansClusterClientFactory clusterClientFactory,
        ILogger<AElfTestAppService> logger)
    {
        _clusterClientFactory = clusterClientFactory;
        _logger = logger;
    }

    public async Task SaveBlock(BlockEventDataDto eventDataDto)
    {
        _logger.LogInformation("Start connect to Silo Server....");
        using (var client = await _clusterClientFactory.GetClient())
        {
            _logger.LogInformation("Prepare Grain Class，While the client IsInitialized:" + client.IsInitialized);
            var blockGrain = client.GetGrain<IBlockGrain>(2);
            var eventData = ObjectMapper.Map<BlockEventDataDto, BlockEventData>(eventDataDto);
            _logger.LogInformation("Start Raise Event of Block Number:" + eventData.BlockNumber);
            await blockGrain.NewEvent(eventData);
        }
        _logger.LogInformation("Stop connect to Silo Server");
    }
}