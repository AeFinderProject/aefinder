using AeFinder.Block.Dtos;
using Orleans;

namespace AeFinder.Grains.Grain.BlockScan;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleNewBlockAsync(BlockWithTransactionDto block);
    Task HandleConfirmedBlockAsync(BlockWithTransactionDto block);
    Task<Guid> InitializeAsync(string chainId, string clientId, string version);
    Task ReScanAsync(long blockHeight);
}