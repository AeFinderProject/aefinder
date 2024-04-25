namespace AeFinder.Sdk.Processor;

public abstract class BlockProcessorBase : BlockDataProcessorBase, IBlockProcessor
{
    public abstract Task ProcessAsync(Block block, BlockContext context);
}