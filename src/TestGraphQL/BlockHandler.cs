using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace GraphQL;

public class BlockHandler : BlockDataHandler<Query>
{
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, string, BlockInfo, Query> _repository;
    private readonly ILogger<BlockHandler> _logger;

    public BlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider<Query> aelfIndexerClientInfoProvider,
        IAElfIndexerClientEntityRepository<TestBlockIndex, string, BlockInfo, Query> repository,
        ILogger<BlockHandler> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider)
    {
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ProcessDataAsync(List<BlockInfo> data)
    {
        foreach (var block in data)
        {
            var index = ObjectMapper.Map<BlockInfo, TestBlockIndex>(block);
            _logger.LogDebug(index.ToJsonString());
            await _repository.AddOrUpdateAsync(index);
        }
        
    }
}