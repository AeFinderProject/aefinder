using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Chains;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Chains;

[Collection(ClusterCollection.Name)]
public class ChainGrainTests : AElfIndexerGrainTestBase
{
    [Fact]
    public async Task SetLatestBlock_Test()
    {
        var chainId = "AELF";
        var blockHash = "blockHash";
        var blockHeight = 100;

        var grain = Cluster.Client.GetGrain<IChainGrain>("AELF");
        await grain.SetLatestBlockAsync(blockHash, blockHeight);
        
        var chainStatus = await grain.GetChainStatusAsync();
        chainStatus.BlockHeight.ShouldBe(blockHeight);
        chainStatus.BlockHash.ShouldBe(blockHash);
        
        await grain.SetLatestBlockAsync("NewBlockHash", 90);
        
        chainStatus = await grain.GetChainStatusAsync();
        chainStatus.BlockHeight.ShouldBe(blockHeight);
        chainStatus.BlockHash.ShouldBe(blockHash);
    }
    
    [Fact]
    public async Task SetLatestConfirmBlock_Test()
    {
        var chainId = "AELF";
        var confirmedBlockHash = "confirmedBlockHash";;
        var confirmedBlockHeight = 80;
        
        var grain = Cluster.Client.GetGrain<IChainGrain>("AELF");
        await grain.SetLatestConfirmBlockAsync(confirmedBlockHash, confirmedBlockHeight);
        
        var chainStatus = await grain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHeight.ShouldBe(confirmedBlockHeight);
        chainStatus.ConfirmedBlockHash.ShouldBe(confirmedBlockHash);
        
        await grain.SetLatestConfirmBlockAsync("NewConfirmedBlockHash", 60);
        chainStatus = await grain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHeight.ShouldBe(confirmedBlockHeight);
        chainStatus.ConfirmedBlockHash.ShouldBe(confirmedBlockHash);
    }
}