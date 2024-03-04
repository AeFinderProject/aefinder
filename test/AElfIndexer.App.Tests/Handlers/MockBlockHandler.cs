using System;
using System.Threading.Tasks;
using AElfIndexer.Sdk;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.App.Handlers;

public class MockBlockHandler : BlockProcessorBase
{
    private readonly IObjectMapper _objectMapper;

    public MockBlockHandler(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task ProcessAsync(Sdk.Block block)
    {
        if (block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var entity = _objectMapper.Map<Sdk.Block, TestBlockIndex>(block);
        await SaveEntityAsync(entity);
    }
}