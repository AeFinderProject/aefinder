namespace AElfIndexer.Sdk.Processor;

public interface IBlockProcessor
{
    Task ProcessAsync(Block block, BlockContext context);
}