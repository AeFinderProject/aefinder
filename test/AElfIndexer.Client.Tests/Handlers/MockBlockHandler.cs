using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Client.BlockHandlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public class MockBlockHandler : BlockDataHandler
{
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public MockBlockHandler(IClusterClient clusterClient, IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IDAppDataProvider dAppDataProvider,
        IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider,
        IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> repository,
        ILogger<MockBlockHandler> logger) : base(clusterClient, objectMapper, aelfIndexerClientInfoProvider,
        dAppDataProvider, blockStateSetProvider, dAppDataIndexManagerProvider, logger)
    {
        _repository = repository;
    }

    protected override async Task ProcessBlockAsync(BlockInfo block)
    {
        if (block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var index = ObjectMapper.Map<BlockInfo, TestBlockIndex>(block);
        Logger.LogDebug(index.ToJsonString());
        await _repository.AddOrUpdateAsync(index);
    }
}