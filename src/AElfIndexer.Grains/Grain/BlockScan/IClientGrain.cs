using AElfIndexer.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IClientGrain: IGrainWithStringKey
{
    Task<string> SubscribeAsync(List<SubscribeInfo> subscribeInfos);

    Task<Guid> GetMessageStreamIdAsync();

    //Task SetBlockScanIdsAsync(string version, HashSet<string> ids);

    Task<bool> IsVersionAvailableAsync(string version);

    Task<string> GetCurrentVersionAsync();
    Task<string> GetNewVersionAsync();
    Task UpgradeVersionAsync();
}