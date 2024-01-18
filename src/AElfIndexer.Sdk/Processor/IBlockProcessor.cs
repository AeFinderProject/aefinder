namespace AElfIndexer.Sdk;

public interface IBlockProcessor : IBlockDataProcessor
{
    Task ProcessAsync(Block block);
}