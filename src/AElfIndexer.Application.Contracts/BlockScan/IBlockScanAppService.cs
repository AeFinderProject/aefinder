using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Streams;

namespace AElfIndexer.BlockScan;

public interface IBlockScanAppService
{
    Task<string> SubmitSubscribeInfoAsync(string clientId, List<SubscribeInfo> subscribeInfos);
    Task<List<string>> GetMessageStreamIdsAsync(string clientId, string version);
    Task StartScanAsync(string clientId, string version);
    Task UpgradeVersionAsync(string clientId);
    Task StopAsync(string clientId, string version);
    Task<bool> IsVersionAvailableAsync(string clientId, string version);
}