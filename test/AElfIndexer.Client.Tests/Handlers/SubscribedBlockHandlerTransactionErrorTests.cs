using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.ScanApps;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandlerTransactionErrorTests : AElfIndexerClientTransactionDataHandlerTestBase
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    
    public SubscribedBlockHandlerTransactionErrorTests()
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
        var clientGrain = _clusterClient.GetGrain<IScanAppGrain>(client);
        
        var currentVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        var newVersion = await clientGrain.AddSubscriptionAsync(new List<SubscriptionInfo>());
        _clientInfoProvider.SetVersion(currentVersion);
        
        await clientGrain.SetTokenAsync(currentVersion);
        await clientGrain.SetTokenAsync(newVersion);
        await _blockScanAppService.StartScanAsync(client, currentVersion);
        var currentVersionToken = await clientGrain.GetTokenAsync(currentVersion);

        var blocks = MockHandlerHelper.CreateBlockWithTransactionDtos(99999, 10, "BlockHash", chainId, 2, "TxId",
            TransactionStatus.Mined);

        await _subscribedBlockHandler.HandleAsync(new SubscribedBlockDto
        {
            Blocks = blocks,
            Token = currentVersionToken,
            Version = currentVersion,
            ChainId = chainId,
            ClientId = client,
            FilterType = BlockFilterType.Transaction
        });

        var state = await clientGrain.GetVersionStatusAsync(currentVersion);
        state.ShouldBe(VersionStatus.Paused);
    }
}