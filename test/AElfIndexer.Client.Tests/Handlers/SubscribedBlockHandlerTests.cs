using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScan;
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

    public SubscribedBlockHandlerTests()
    {
        _subscribedBlockHandler = GetRequiredService<ISubscribedBlockHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task Handle_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(client);
        
        var currentVersion = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>());
        var newVersion = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>());
        _clientInfoProvider.SetVersion(currentVersion);
        
        await clientGrain.SetTokenAsync(currentVersion);
        await clientGrain.SetTokenAsync(newVersion);
        var currentVersionToken = await clientGrain.GetTokenAsync(currentVersion);
        var newVersionToken = await clientGrain.GetTokenAsync(newVersion);
        
        var key = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, currentVersion);
        var grain = _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(key);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = new List<BlockWithTransactionDto>(),
            Token = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Block
        });
        var bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        var blocks = MockHandlerHelper.CreateBlock(10000, 10, "BlockHash", chainId);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            ClientId = "WrongClient",
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = currentVersionToken,
            Version = "WrongVersion",
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = "WrongToken",
            Version = currentVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();
        
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("BlockHash",10100);
        
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Block
        });
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.BlockHeight.ShouldBe(10009);

        var versionInfo = await clientGrain.GetVersionAsync();
        versionInfo.CurrentVersion.ShouldBe(currentVersion);
        versionInfo.NewVersion.ShouldBe(newVersion);
        
        _clientInfoProvider.SetVersion(newVersion);
        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = newVersionToken,
            Version = newVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Block
        });
        key = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, newVersion);
        grain = _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(key);
        bestChain = await grain.GetBestChainBlockStateSetAsync();
        bestChain.BlockHeight.ShouldBe(10009);
        
        versionInfo = await clientGrain.GetVersionAsync();
        versionInfo.CurrentVersion.ShouldBe(newVersion);
        versionInfo.NewVersion.ShouldBeNull();
    }
}