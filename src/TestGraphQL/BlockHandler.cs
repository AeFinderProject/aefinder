using AeFinder.Client;
using AeFinder.Client.Handlers;
using AeFinder.Client.Providers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace GraphQL;

public class BlockHandler : BlockDataHandler
{
    private readonly IAeFinderClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public BlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAeFinderClientInfoProvider aefinderClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IAeFinderClientEntityRepository<TestBlockIndex, BlockInfo> repository,
        ILogger<BlockHandler> logger) 
        : base(clusterClient, objectMapper, aefinderClientInfoProvider, dAppDataProvider,
        blockStateSetProvider, dAppDataIndexManagerProvider,logger)
    {
        _repository = repository;
    }

    protected override async Task ProcessDataAsync(List<BlockInfo> data)
    {
        foreach (var block in data)
        {
            var index = ObjectMapper.Map<BlockInfo, TestBlockIndex>(block);
            Logger.LogDebug(index.ToJsonString());
            await _repository.AddOrUpdateAsync(index);
        }
        
    }

    protected override Task ProcessBlocksAsync(List<BlockInfo> data)
    {
        throw new NotImplementedException();
    }
}