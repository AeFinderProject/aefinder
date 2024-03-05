using System;
using System.Threading.Tasks;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.App;

public class MockBlockProcessor : BlockProcessorBase, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;

    public MockBlockProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task ProcessAsync(Sdk.Block block)
    {
        if (block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var entity = _objectMapper.Map<Sdk.Block, TestBlockEntity>(block);
        entity.Id = block.BlockHash;
        await SaveEntityAsync(entity);
    }
}