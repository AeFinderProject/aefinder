using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IClientGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionInfoAsync(Subscription subscription);
    Task UpdateSubscriptionInfoAsync(string version, Subscription subscription);
    Task<Subscription> GetSubscriptionAsync(string version);
    Task<SubscriptionInfoDto> GetAllSubscriptionAsync();
    Task<bool> IsRunningAsync(string version, string chainId, string scanToken);
    Task UpgradeVersionAsync();
    Task<VersionStatus> GetVersionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task StopAsync(string version);
}