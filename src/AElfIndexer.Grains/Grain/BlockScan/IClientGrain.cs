using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IClientGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionInfoAsync(Subscription subscription);
    Task UpdateSubscriptionInfoAsync(string version, Subscription subscription);
    Task<Subscription> GetSubscriptionAsync(string version);
    Task AddBlockScanIdAsync(string version, string id);
    Task<List<string>> GetBlockScanIdsAsync(string version);
    Task<bool> IsRunningAsync(string version, string token);
    Task UpgradeVersionAsync();
    Task RemoveVersionInfoAsync(string version);
    Task<VersionStatus> GetVersionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task<ClientVersion> GetVersionAsync();
    Task SetTokenAsync(string version);
    Task<string> GetTokenAsync(string version);
    Task StopAsync(string version);
}