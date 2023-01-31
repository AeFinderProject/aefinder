using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Client;

[Collection(ClusterCollection.Name)]
public class BlockStateSetGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task BlockStateSetsTest()
    {
        var blockStateSetGrain =
            Cluster.Client.GetGrain<IBlockStateSetGrain<int>>("id");

        var sets = await blockStateSetGrain.GetBlockStateSetsAsync();
        sets.Count.ShouldBe(0);

        var longestChain = await blockStateSetGrain.GetLongestChainBlockStateSetAsync();
        longestChain.ShouldBeNull();

        var bestChain = await blockStateSetGrain.GetBestChainBlockStateSetAsync();
        bestChain.ShouldBeNull();

        var blockSets = new Dictionary<string, BlockStateSet<int>>();
        for (int i = 0; i < 16; i++)
        {
            var blockHash = "BlockHash" + (101 + i);
            blockSets.Add(blockHash,new BlockStateSet<int>
            {
                BlockHash = blockHash,
                BlockHeight = 101 + i ,
                PreviousBlockHash = "BlockHash" + (100 + i),
                Confirmed = false,
                Data = new List<int>(),
                Changes = new Dictionary<string, string> { { "key", "value" } }
            });
        }

        await blockStateSetGrain.SetBlockStateSetsAsync(blockSets);

        sets = await blockStateSetGrain.GetBlockStateSetsAsync();
        sets.Count.ShouldBe(16);

        await blockStateSetGrain.SetLongestChainBlockHashAsync("BlockHash115");
        longestChain = await blockStateSetGrain.GetLongestChainBlockStateSetAsync();
        longestChain.BlockHash.ShouldBe("BlockHash115");
        
        await blockStateSetGrain.SetBestChainBlockHashAsync("BlockHash115");
        bestChain = await blockStateSetGrain.GetBestChainBlockStateSetAsync();
        bestChain.BlockHash.ShouldBe("BlockHash115");
    }
}