using System;
using System.Threading.Tasks;
using AeFinder.Sdk.Processor;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.App.MockApp;

public class BlockProcessor : BlockProcessorBase, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;

    public BlockProcessor(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task ProcessAsync(Sdk.Processor.Block block)
    {
        if (block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var entity = _objectMapper.Map<Sdk.Processor.Block, BlockEntity>(block);
        entity.Id = block.BlockHash;
        await SaveEntityAsync(entity);
    }
}