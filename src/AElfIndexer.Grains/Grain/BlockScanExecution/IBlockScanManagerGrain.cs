using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public interface IBlockScanManagerGrain : IGrainWithIntegerKey
{
    Task<List<string>> GetBlockScanIdsByChainAsync(string chainId);
    Task<Dictionary<string, HashSet<string>>> GetAllBlockScanIdsAsync();
    Task AddBlockScanAsync(string chainId, string blockScanId);
    Task RemoveBlockScanAsync(string chainId, string blockScanId);
}