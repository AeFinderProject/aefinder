using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.BlockScan;

public interface IBlockScanAppService
{
    Task<List<Guid>> GetMessageStreamIdsAsync(string appId, string version, string chainId = null);
    Task StartScanAsync(string appId, string version, string chainId = null);
    Task UpgradeVersionAsync(string appId, string version);
    Task StopAsync(string appId, string version);
    Task<AllSubscriptionDto> GetSubscriptionAsync(string appId);
    Task PauseAsync(string appId, string version);
    Task<bool> IsRunningAsync(string chainId, string appId, string version, string token);
}