namespace AElfIndexer.Client;

public interface IBlockProcessingContext
{
    public string ScanAppId { get; }
    public string Version { get; }
    public string ChainId { get; }
}

// public class BlockProcessingContext : IBlockProcessingContext
// {
//     
// }