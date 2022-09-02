using System;
using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Etos;
using AElfScan.EventData;
using AElfScan.Grain;
using AElfScan.Orleans;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.AElf;

public class AElfTestAppService:AElfScanAppService,IAElfAppService
{
    private readonly IOrleansClusterClientFactory _clusterClientFactory;
    private readonly ILogger<AElfTestAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;

    public AElfTestAppService(
        IOrleansClusterClientFactory clusterClientFactory,
        ILogger<AElfTestAppService> logger,
        IDistributedEventBus distributedEventBus)
    {
        _clusterClientFactory = clusterClientFactory;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
    }

    public async Task SaveBlock(BlockEventDataDto eventDataDto)
    {
        _logger.LogInformation("Start connect to Silo Server....");
        using (var client = await _clusterClientFactory.GetClient())
        {
            _logger.LogInformation("Prepare Grain Class，While the client IsInitialized:" + client.IsInitialized);
            var blockGrain = client.GetGrain<IBlockGrain>(3);
            var eventData = ObjectMapper.Map<BlockEventDataDto, BlockEventData>(eventDataDto);
            _logger.LogInformation("Start Raise Event of Block Number:" + eventData.BlockNumber);
            await blockGrain.NewEvent(eventData);
        }
        _logger.LogInformation("Stop connect to Silo Server");
        
        _logger.LogInformation("Start publish Event to Rabbitmq");
        await _distributedEventBus.PublishAsync(
            new BlockTestEto
            {
                Id = Guid.NewGuid(),
                BlockNumber = eventDataDto.BlockNumber,
                BlockTime = DateTime.Now,
                IsConfirmed = eventDataDto.IsConfirmed
            }
            );
        _logger.LogInformation("Test Block Event is already published");
    }
}
