using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.BlockScan;

public interface IBlockScanAppService
{
    Task<string> SubmitSubscriptionInfoAsync(string clientId, List<SubscriptionInfo> subscriptionInfos);
    Task UpdateSubscriptionInfoAsync(string clientId, string version, List<SubscriptionInfo> subscriptionInfos);
    Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version);
    Task StartScanAsync(string clientId, string version);
    Task UpgradeVersionAsync(string clientId);
    Task<ClientVersionDto> GetClientVersionAsync(string clientId);
    Task<string> GetClientTokenAsync(string clientId, string version);
    Task StopAsync(string clientId, string version);
    Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(string clientId);
}