using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Studio;

namespace AeFinder.BlockScan;

public interface IBlockScanAppService
{
    Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version);
    Task StartScanAsync(string clientId, string version);
    Task UpgradeVersionAsync(string clientId);
    Task StopAsync(string clientId, string version);
    Task<AllSubscriptionDto> GetSubscriptionAsync(string clientId);
    Task PauseAsync(string clientId, string version);
    Task<bool> IsRunningAsync(string chainId, string clientId, string version, string token);
}