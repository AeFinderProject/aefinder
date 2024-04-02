namespace AeFinder.BlockGrainManager;

public interface IBlockDataSynchronizer
{
    Task SyncConfirmedBlockAsync(string chainId, long startHeight, long syncEndHeight);
}