using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.BlockScan;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanInfoGrainTests : AeFinderGrainTestBase
{
    [Fact]
    public async Task Initialize_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = "Version";
        var subscriptionInfo = new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 100,
            SubscribeEvents = new List<FilterContractEventInput>
            {
                new FilterContractEventInput
                {
                    ContractAddress = "ContractAddress",
                    EventNames = new List<string>{"EventName"}
                }
            }
        };
        
        var clientGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(clientId);
        await clientGrain.InitializeAsync(chainId, clientId,  version,subscriptionInfo);
        var clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.ChainId.ShouldBe(chainId);
        clientInfo.ClientId.ShouldBe(clientId);
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.HistoricalBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(0);

        var subscribe = await clientGrain.GetSubscriptionInfoAsync();
        subscribe.ChainId.ShouldBe(subscriptionInfo.ChainId);
        subscribe.OnlyConfirmedBlock.ShouldBe(subscriptionInfo.OnlyConfirmedBlock);
        subscribe.StartBlockNumber.ShouldBe(subscriptionInfo.StartBlockNumber);
        subscribe.SubscribeEvents.Count.ShouldBe(1);
        subscribe.SubscribeEvents[0].ContractAddress.ShouldBe(subscriptionInfo.SubscribeEvents[0].ContractAddress);
        subscribe.SubscribeEvents[0].EventNames.Count.ShouldBe(1);
        subscribe.SubscribeEvents[0].EventNames[0].ShouldBe(subscriptionInfo.SubscribeEvents[0].EventNames[0]);

        var clientManagerGrain = Cluster.Client.GetGrain<IBlockScanManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);

        var allClientIds = await clientManagerGrain.GetAllBlockScanIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);

        await clientGrain.SetScanNewBlockStartHeightAsync(80);
        clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.NewBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(80);

        clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);
        
        allClientIds = await clientManagerGrain.GetAllBlockScanIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);
        
        await clientGrain.StopAsync();

        clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(0);
        
        allClientIds = await clientManagerGrain.GetAllBlockScanIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(0);
        
        clientIds = await clientManagerGrain.GetBlockScanIdsByChainAsync("tDVV");
        clientIds.Count.ShouldBe(0);
    }
}