using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Services;
using AElfIndexer.App.Handlers;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Client.BlockHandlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Apps;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandlerTests : AElfIndexerClientBlockDataHandlerTestBase
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;

    public SubscribedBlockHandlerTests()
    {
        _subscribedBlockHandler = GetRequiredService<ISubscribedBlockHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
    }

    [Fact]
    public async Task Handle_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var clientGrain = _clusterClient.GetGrain<IAppGrain>(client);
        
        var currentVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        var newVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        _clientInfoProvider.SetVersion(currentVersion);

        await _blockScanAppService.StartScanAsync(client, currentVersion);
        await clientGrain.SetTokenAsync(newVersion);
        var currentVersionToken = await clientGrain.GetTokenAsync(currentVersion);
        var newVersionToken = await clientGrain.GetTokenAsync(newVersion);
        
        var key = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, currentVersion);
        var grain = _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(key);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = new List<BlockWithTransactionDto>(),
            PushToken = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });
        var bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        var blocks = MockHandlerHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = "WrongClient",
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = currentVersionToken,
            Version = "WrongVersion",
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = "WrongToken",
            Version = currentVersion,
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("BlockHash",10100);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.BlockHeight.ShouldBe(10009);

        var versionInfo = await clientGrain.GetVersionAsync();
        versionInfo.CurrentVersion.ShouldBe(currentVersion);
        versionInfo.NewVersion.ShouldBe(newVersion);
        
        await _blockScanAppService.StartScanAsync(client, newVersion);
        _clientInfoProvider.SetVersion(newVersion);
        newVersionToken = await clientGrain.GetTokenAsync(newVersion);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = newVersionToken,
            Version = newVersion,
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });
        key = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, newVersion);
        grain = _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(key);
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.BlockHeight.ShouldBe(10009);

        await _blockScanAppService.UpgradeVersionAsync(client);
        versionInfo = await clientGrain.GetVersionAsync();
        versionInfo.CurrentVersion.ShouldBe(newVersion);
        versionInfo.NewVersion.ShouldBeNull();
    }
    
    [Fact]
    public async Task Handle_Block_Error_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var clientGrain = _clusterClient.GetGrain<IAppGrain>(client);
        
        var currentVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        var newVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        _clientInfoProvider.SetVersion(currentVersion);
        
        await clientGrain.SetTokenAsync(currentVersion);
        await clientGrain.SetTokenAsync(newVersion);
        await _blockScanAppService.StartScanAsync(client, currentVersion);
        var currentVersionToken = await clientGrain.GetTokenAsync(currentVersion);

        var blocks = MockHandlerHelper.CreateBlock(99999, 10, "BlockHash", chainId);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            PushToken = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            AppId = client,
            FilterType = BlockFilterType.Block
        });

        var state = await clientGrain.GetVersionStatusAsync(currentVersion);
        state.ShouldBe(VersionStatus.Paused);
    }
}