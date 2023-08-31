using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandlerLogEventErrorTests : AElfIndexerClientLogEventHandlerTestBase
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    
    public SubscribedBlockHandlerLogEventErrorTests()
    {
        _subscribedBlockHandler = GetRequiredService<ISubscribedBlockHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
    }
    
    [Fact]
    public async Task Handle_Transaction_Error_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(client);
        
        var currentVersion = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>());
        var newVersion = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>());
        _clientInfoProvider.SetVersion(currentVersion);
        
        await clientGrain.SetTokenAsync(currentVersion);
        await clientGrain.SetTokenAsync(newVersion);
        await _blockScanAppService.StartScanAsync(client, currentVersion);
        var currentVersionToken = await clientGrain.GetTokenAsync(currentVersion);

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtosAndTransferredLogEvent(99999, 10, "BlockHash",
            chainId, 2, "TxId",
            TransactionStatus.Mined, 1);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.LogEvent
        });

        var state = await clientGrain.GetVersionStatusAsync(currentVersion);
        state.ShouldBe(VersionStatus.Paused);
    }
}