using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Streams;

namespace AElfIndexer.BlockScan;

public interface IBlockScanAppService
{
    Task<string> SubmitSubscriptionInfoAsync(string clientId, Subscription subscriptionInfos);
    //Task UpdateSubscriptionInfoAsync(string clientId, string version, List<SubscriptionInfo> subscriptionInfos);
    Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version);
    Task StartScanAsync(string clientId, string version);
    Task UpgradeVersionAsync(string clientId);
    Task StopAsync(string clientId, string version);
    Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(string clientId);
    Task PauseAsync(string clientId, string version);
    Task<bool> IsRunningAsync(string chainId, string clientId, string version, string token);
}