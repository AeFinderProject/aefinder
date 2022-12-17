using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Xunit;

namespace AElfIndexer.BlockScan;

public class BlockScanAppServiceTests : AElfIndexerApplicationOrleansTestBase
{
    private IBlockScanAppService _blockScanAppService;
    private IClusterClient _clusterClient;

    public BlockScanAppServiceTests()
    {
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task SubscriptionInfoTest()
    {
        var clientId = "Client";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block
            }
        };

        var version1 = await _blockScanAppService.SubmitSubscriptionInfoAsync(clientId, subscriptionInfo1);
        
        
    }
}