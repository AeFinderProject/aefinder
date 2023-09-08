using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace GraphQL;

public class BlockHandler : BlockDataHandler
{
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public BlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> repository,
        ILogger<BlockHandler> logger) 
        : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider, dAppDataProvider,
        blockStateSetProvider, dAppDataIndexManagerProvider,logger)
    {
        _repository = repository;
    }

    protected override async Task ProcessDataAsync(string chainId, List<BlockInfo> data)
    {
        foreach (var block in data)
        {
            var index = ObjectMapper.Map<BlockInfo, TestBlockIndex>(block);
            Logger.LogDebug(index.ToJsonString());
            await _repository.AddOrUpdateAsync(index);
        }
        
    }

    protected override Task ProcessBlockAsync(BlockInfo data)
    {
        throw new NotImplementedException();
    }
}