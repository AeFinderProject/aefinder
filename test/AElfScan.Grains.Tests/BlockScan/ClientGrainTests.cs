using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
using AElfScan.Grains.Grain.BlockScan;
using AElfScan.Grains.State.BlockScan;
using Shouldly;
using Xunit;

namespace AElfScan.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class ClientGrainTests : AElfScanGrainTestBase
{
    [Fact]
    public async Task InitializeTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = "Version";
        var subscribeInfo = new SubscribeInfo
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
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        await clientGrain.InitializeAsync(chainId, clientId,  version,subscribeInfo);
        var clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.Version.ShouldBe(version);
        clientInfo.ChainId.ShouldBe(chainId);
        clientInfo.ClientId.ShouldBe(clientId);
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.HistoricalBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(0);

        var subscribe = await clientGrain.GetSubscribeInfoAsync();
        subscribe.ChainId.ShouldBe(subscribeInfo.ChainId);
        subscribe.OnlyConfirmedBlock.ShouldBe(subscribeInfo.OnlyConfirmedBlock);
        subscribe.StartBlockNumber.ShouldBe(subscribeInfo.StartBlockNumber);
        subscribe.SubscribeEvents.Count.ShouldBe(1);
        subscribe.SubscribeEvents[0].ContractAddress.ShouldBe(subscribeInfo.SubscribeEvents[0].ContractAddress);
        subscribe.SubscribeEvents[0].EventNames.Count.ShouldBe(1);
        subscribe.SubscribeEvents[0].EventNames[0].ShouldBe(subscribeInfo.SubscribeEvents[0].EventNames[0]);

        var clientManagerGrain = Cluster.Client.GetGrain<IClientManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetClientIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);

        var allClientIds = await clientManagerGrain.GetAllClientIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);

        await clientGrain.SetScanNewBlockStartHeightAsync(80);
        clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.NewBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(80);

        await clientGrain.StopAsync(Guid.NewGuid().ToString());
        clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.Version.ShouldBe(version);
        
        clientIds = await clientManagerGrain.GetClientIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);
        
        allClientIds = await clientManagerGrain.GetAllClientIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);
        
        await clientGrain.StopAsync(version);
        clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.Version.ShouldNotBe(version);
        
        clientIds = await clientManagerGrain.GetClientIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(0);
        
        allClientIds = await clientManagerGrain.GetAllClientIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(0);
    }
}