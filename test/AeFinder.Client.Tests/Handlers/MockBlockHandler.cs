using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Client.Providers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Client.Handlers;

public class MockBlockHandler : BlockDataHandler
{
    private readonly IAeFinderClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public MockBlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAeFinderClientInfoProvider aefinderClientInfoProvider,
        IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IAeFinderClientEntityRepository<TestBlockIndex, BlockInfo> repository,
        ILogger<MockBlockHandler> logger) : base(clusterClient, objectMapper, aefinderClientInfoProvider,
        dAppDataProvider, blockStateSetProvider, dAppDataIndexManagerProvider, logger)
    {
        _repository = repository;
    }
    
    protected override async Task ProcessBlocksAsync(List<BlockInfo> data)
    {
        foreach (var block in data)
        {
            var index = ObjectMapper.Map<BlockInfo, TestBlockIndex>(block);
            Logger.LogDebug(index.ToJsonString());
            await _repository.AddOrUpdateAsync(index);
        }
    }
}