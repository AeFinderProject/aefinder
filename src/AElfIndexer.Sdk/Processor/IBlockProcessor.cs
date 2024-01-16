namespace AElfIndexer.Sdk;

public interface IBlockProcessor
{
    Task ProcessAsync(Block block);
}