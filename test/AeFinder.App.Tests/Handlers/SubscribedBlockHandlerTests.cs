using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.BlockProcessing;
using AeFinder.Apps;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Orleans.TestBase;
using Shouldly;
using Xunit;

namespace AeFinder.App.Handlers;

[Collection(ClusterCollection.Name)]
public class SubscribedBlockHandlerTests : AeFinderAppTestBase
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    // private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IProcessingStatusProvider _processingStatusProvider;

    public SubscribedBlockHandlerTests()
    {
        _subscribedBlockHandler = GetRequiredService<ISubscribedBlockHandler>();
        // _clusterClient = GetRequiredService<IClusterClient>();
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _processingStatusProvider = GetRequiredService<IProcessingStatusProvider>();
    }

    [Fact]
    public async Task Handle_Test()
    {
        var chainId = "AELF";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(currentVersion);

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        _processingStatusProvider.SetStatus(chainId, ProcessingStatus.Running);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = "WrongClient"
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = "WrongVersion",
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = "WrongToken",
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, currentVersion);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = new List<AppSubscribedBlockDto>(),
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
        
        blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        pushToken = blockPushInfoCurrentVersion.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(10009);
        
        var isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Handle_Block_Error_Test()
    {
        var chainId = "AELF";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 100000
                }
            }
        }, new byte[] { })).NewVersion;

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(100000, 10, "BlockHash", chainId);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, currentVersion);
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(0);

        var isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeFalse();
    }
    
    [Fact]
    public async Task Handle_Block_HandleFailed_Test()
    {
        var chainId = "AELF";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, currentVersion);
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;
        
        _processingStatusProvider.SetStatus(chainId, ProcessingStatus.Failed);
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WrongChaindId_Test()
    {
        var chainId = "tDVV";
        var appGrain =
            Cluster.Client.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));

        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(currentVersion);

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));

        _appInfoProvider.SetChainId(chainId);
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        _processingStatusProvider.SetStatus(chainId, ProcessingStatus.Running);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = "AELF",
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
    }
    
    [Fact]
    public async Task Handle_MultiChain_Test()
    {
        var chainId = "AELF";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                },
                new Subscription
                {
                    ChainId = "tDVV",
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(currentVersion);

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        _processingStatusProvider.SetStatus(chainId,ProcessingStatus.Running);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, currentVersion, chainId);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = new List<AppSubscribedBlockDto>(),
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainBlockHash.ShouldBeNull();
        
        blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        pushToken = blockPushInfoCurrentVersion.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(10009);
        
        var isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Handle_Upgrade_Test()
    {
        var chainId = "AELF";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        
        var pendingVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(pendingVersion);

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, pendingVersion, chainId));

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId,confirmed: true);
        _processingStatusProvider.SetStatus(chainId,ProcessingStatus.Running);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, pendingVersion);
        
        var blockPushInfo = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfo.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = pendingVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(10009);
        
        var isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeTrue();

        var subscriptions = await appGrain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.Version.ShouldBe(pendingVersion);
        subscriptions.PendingVersion.ShouldBeNull();
    }
    
    [Fact]
    public async Task Handle_Upgrade_MultiChain_Test()
    {
        var chainId = "AELF";
        var sideChainId = "tDVV";
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        
        var currentVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                },
                new Subscription
                {
                    ChainId = "tDVV",
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        
        var pendingVersion = (await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                },
                new Subscription
                {
                    ChainId = "tDVV",
                    StartBlockNumber = 10000
                }
            }
        }, new byte[] { })).NewVersion;
        _appInfoProvider.SetVersion(pendingVersion);
        _appInfoProvider.SetChainId(chainId);
        
        var aelfBlockStateSetStatusGrain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, currentVersion, chainId));
        await aelfBlockStateSetStatusGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
            { LastIrreversibleBlockHeight = 11000 });
        var tdvvBlockStateSetStatusGrain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, currentVersion, sideChainId));
        await tdvvBlockStateSetStatusGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
            { LastIrreversibleBlockHeight = 11000 });

        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, pendingVersion, chainId));

        var grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId,confirmed: true);
        _processingStatusProvider.SetStatus(chainId,ProcessingStatus.Running);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, pendingVersion, chainId);
        
        var blockPushInfo = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfo.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = pendingVersion,
            ChainId = chainId,
            AppId = _appInfoProvider.AppId
        });
        var blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(10009);
        
        var isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeTrue();

        var subscriptions = await appGrain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.Version.ShouldBe(currentVersion);
        subscriptions.PendingVersion.Version.ShouldBe(pendingVersion);

       
        _appInfoProvider.SetChainId(sideChainId);
        _processingStatusProvider.SetStatus(sideChainId,ProcessingStatus.Running);

        blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, pendingVersion, sideChainId));

        grain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                sideChainId));
        
        blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", sideChainId,confirmed: true);
        _processingStatusProvider.SetStatus(chainId,ProcessingStatus.Running);
        
        await _blockScanAppService.StartScanAsync(_appInfoProvider.AppId, pendingVersion, sideChainId);
        
        blockPushInfo = await blockPusherInfoGrain.GetPushInfoAsync();
        pushToken = blockPushInfo.PushToken;
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = pushToken,
            Version = pendingVersion,
            ChainId = sideChainId,
            AppId = _appInfoProvider.AppId
        });
        blockStateSetStatus = await grain.GetBlockStateSetStatusAsync();
        blockStateSetStatus.BestChainHeight.ShouldBe(10009);
        
        isRunning = _processingStatusProvider.IsRunning(chainId);
        isRunning.ShouldBeTrue();

        subscriptions = await appGrain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.Version.ShouldBe(pendingVersion);
        subscriptions.PendingVersion.ShouldBeNull();
    }
}