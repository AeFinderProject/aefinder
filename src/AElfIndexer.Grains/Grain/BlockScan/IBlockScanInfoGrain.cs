using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockScanInfoGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscribeInfo> GetSubscribeInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task InitializeAsync(string chainId, string clientId, string version, SubscribeInfo info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
}