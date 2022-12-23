using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace GraphQL;

public class BlockHandler : BlockDataHandler<Query>
{
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, string, BlockInfo, Query> _repository;

    public BlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider<Query> aelfIndexerClientInfoProvider,
        IAElfIndexerClientEntityRepository<TestBlockIndex, string, BlockInfo, Query> repository,
        ILogger<BlockHandler> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider,logger)
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