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

public class BlockAttachServiceUnlinkedTests : AeFinderAppUnlinkedBlockTestBase
{
    private readonly IBlockAttachService _blockAttachService;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly ILocalEventBus _localEventBus;

    public BlockAttachServiceUnlinkedTests()
    {
        _blockAttachService = GetRequiredService<IBlockAttachService>();
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _localEventBus = GetRequiredService<ILocalEventBus>();
    }
    
    [Fact]
    public async Task AttachBlocks_Unlinked_Test()
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
        var version = (await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { })).NewVersion;
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
            
            var blocks = BlockCreationHelper.CreateBlock(120, 10, "BlockHash", chainId, "BlockHash119");
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.BlockHash.ShouldBe("BlockHash129");
            longestChainFoundEventData.BlockHeight.ShouldBe(129);
            lastIrreversibleBlockStateSetFoundEventData.ShouldBeNull();
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(30);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash129");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(129);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.ShouldBeNull();
        }
    }
    
    [Fact]
    public async Task AttachBlocks_Unlinked_StartBlock_Test()
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
        var version = (await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(version); 

        {
            var blocks = BlockCreationHelper.CreateBlock(110, 10, "BlockHash", chainId);
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
    }
    
    [Fact]
    public async Task AttachBlocks_Unlinked_CofirmedBlock_Test()
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
        var version = (await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { })).NewVersion;
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

            var blocks =
                BlockCreationHelper.CreateBlock(105, 10, "BlockHash", chainId, "BlockHash104", confirmed: true);
            await _blockAttachService.AttachBlocksAsync(chainId, blocks);

            longestChainFoundEventData.BlockHash.ShouldBe("BlockHash114");
            longestChainFoundEventData.BlockHeight.ShouldBe(114);
            lastIrreversibleBlockStateSetFoundEventData.BlockHash.ShouldBe("BlockHash114");
            lastIrreversibleBlockStateSetFoundEventData.BlockHeight.ShouldBe(114);
            
            var blockStateSetCount = await _appBlockStateSetProvider.GetBlockStateSetCountAsync(chainId);
            blockStateSetCount.ShouldBe(1);
            
            var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
            longestChainBlockStateSet.Block.BlockHash.ShouldBe("BlockHash114");
            longestChainBlockStateSet.Block.BlockHeight.ShouldBe(114);
            
            var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
            lastIrreversibleBlockStateSet.Block.BlockHash.ShouldBe("BlockHash114");
            lastIrreversibleBlockStateSet.Block.BlockHeight.ShouldBe(114);
        }
    }
}