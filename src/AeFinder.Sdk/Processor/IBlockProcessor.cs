namespace AeFinder.Sdk.Processor;

public interface IBlockProcessor : IBlockDataProcessor
{
    Task ProcessAsync(Block block);
}