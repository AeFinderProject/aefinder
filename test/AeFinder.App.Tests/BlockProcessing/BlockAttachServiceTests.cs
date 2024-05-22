using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AeFinder.App.BlockProcessing;

public class BlockAttachServiceTests : AeFinderAppTestBase
{
    private readonly IBlockAttachService _blockAttachService;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly ILocalEventBus _localEventBus;

    public BlockAttachServiceTests()
    {
        _blockAttachService = GetRequiredService<IBlockAttachService>();
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _localEventBus = GetRequiredService<ILocalEventBus>();
    }

    [Fact]
    public async Task AttachBlocks_Test()
    {
        var chainId = "AELF";
        LongestChainFoundEventData longestChainFoundEventData = null;
        LastIrreversibleBlockStateSetFoundEventData lastIrreversibleBlockStateSetFoundEventData = null;

        _localEventBus.Subscribe<LongestChainFoundEventData>(d =>
        {
            longestChainFoundEventData = d;
            return Task.CompletedTask;
        });
        _localEventBus.Subscribe<LastIrreversibleBlockStateSetFoundEventData>(d =>
        {
            lastIrreversibleBlockStateSetFoundEventData = d;
            return Task.CompletedTask;
        });
        
        var subscriptionManifest1 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 100
                }
            }
        };
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        var version = await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { });
        _appInfoProvider.SetVersion(version); 

        {
            var blocks = BlockCreationHelper.CreateBlock(100, 10, "BlockHash", chainId);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.BlockHash.ShouldBe("BlockHash109");
            longestChainFoundEventData.BlockHeight.ShouldBe(109);
            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(10);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash109");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(109);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.ShouldBeNull();
        }

        {
            longestChainFoundEventData = null;
            lastIrreversibleBlockStateSetFoundEventData = null;
            
            var blocks = BlockCreationHelper.CreateBlock(110, 10, "BlockHash", chainId, "BlockHash109");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.BlockHash.ShouldBe("BlockHash119");
            longestChainFoundEventData.BlockHeight.ShouldBe(119);
            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(20);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash119");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(119);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.ShouldBeNull();
        }
        
        {
            longestChainFoundEventData = null;
            lastIrreversibleBlockStateSetFoundEventData = null;
            
            var blocks = BlockCreationHelper.CreateBlock(110, 10, "BlockHash1000", chainId,"BlockHash10");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.ShouldBeNull();
            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(20);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash119");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(119);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.ShouldBeNull();
        }
        
        {
            longestChainFoundEventData = null;
            lastIrreversibleBlockStateSetFoundEventData = null;
            
            var blocks = BlockCreationHelper.CreateBlock(100, 10, "BlockHash", chainId, confirmed: true);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.ShouldBeNull();
            lastIrreversibleBlockStateSetFoundEventData.BlockHash.ShouldBe("BlockHash109");
            lastIrreversibleBlockStateSetFoundEventData.BlockHeight.ShouldBe(109);
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(11);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash119");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(119);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.Block.BlockHash.ShouldBe("BlockHash109");
            lastIrreversibleBlockStateSet.Block.BlockHeight.ShouldBe(109);
        }
        
        {
            longestChainFoundEventData = null;
            lastIrreversibleBlockStateSetFoundEventData = null;
            
            var blocks = BlockCreationHelper.CreateBlock(115, 2, "BlockHashA", chainId, "BlockHash114");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.ShouldBeNull();
            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(13);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash119");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(119);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.Block.BlockHash.ShouldBe("BlockHash109");
            lastIrreversibleBlockStateSet.Block.BlockHeight.ShouldBe(109);
            
            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHashA115");
            blockStateSet.ShouldNotBeNull();
            blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, "BlockHashA116");
            blockStateSet.ShouldNotBeNull();
        }
    }
}