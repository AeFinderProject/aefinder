using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.AElf;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Handlers;

public class MockBlockHandler : BlockDataHandler
{
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public MockBlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> repository,
        ILogger<MockBlockHandler> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider,logger)
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