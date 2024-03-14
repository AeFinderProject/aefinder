using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.App.BlockProcessing;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockPush;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.Subscriptions;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.App.Handlers;

public class SubscribedBlockHandlerTests : AElfIndexerAppTestBase
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IProcessingStatusProvider _processingStatusProvider;

    public SubscribedBlockHandlerTests()
    {
        _subscribedBlockHandler = GetRequiredService<ISubscribedBlockHandler>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _processingStatusProvider = GetRequiredService<IProcessingStatusProvider>();
    }

    [Fact]
    public async Task Handle_Test()
    {
        var chainId = "AELF";
        var appGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appInfoProvider.AppId));
        
        var currentVersion = await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        });

        var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));
        var blockPushInfoCurrentVersion = await blockPusherInfoGrain.GetPushInfoAsync();
        var pushToken = blockPushInfoCurrentVersion.PushToken;

        var grain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        
        var blocks = BlockCreationHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        
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
            Blocks = new List<BlockWithTransactionDto>(),
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
        var appGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appInfoProvider.AppId));
        
        var currentVersion = await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 100000
                }
            }
        });

        var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));

        var grain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
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
        var appGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(_appInfoProvider.AppId));
        
        var currentVersion = await appGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = chainId,
                    StartBlockNumber = 10000
                }
            }
        });

        var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(
            GrainIdHelper.GenerateBlockPusherGrainId(_appInfoProvider.AppId, currentVersion, chainId));

        var grain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
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
}