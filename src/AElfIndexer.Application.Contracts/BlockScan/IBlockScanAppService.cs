using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElfIndexer.BlockScan;

public interface IBlockScanAppService
{
    Task SubscribeAsync(string clientId, List<SubscribeInfo> subscribeInfos);
}