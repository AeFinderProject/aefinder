using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Client;

[Collection(ClusterCollection.Name)]
public class BlockStateSetsGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task BlockStateSetsTest()
    {
        // var blockStateSetsGrain =
        //     Cluster.Client.GetGrain<IBlockStateSetsGrain<int>>("id");
        //
        // var blockStateSet = new BlockStateSet<int>
        // {
        //     BlockHash = "BlockHash101",
        //     BlockHeight = 101,
        //     PreviousBlockHash = "BlockHash100",
        //     Confirmed = false,
        //     Data = new List<int>(),
        //     Changes = new Dictionary<string, string> { { "key", "value" } }
        // };
        //
        // await blockStateSetsGrain.AddBlockStateSet(blockStateSet);
        // var stateSets = await blockStateSetsGrain.GetBlockStateSets();
        // stateSets.Count.ShouldBe(1);
        // stateSets[blockStateSet.BlockHash].BlockHash.ShouldBe(blockStateSet.BlockHash);
        // stateSets[blockStateSet.BlockHash].Processed.ShouldBeFalse();
        //
        // await blockStateSetsGrain.SetBlockStateSetProcessed("NotExist");
        // var processedStateSet = await blockStateSetsGrain.GetBlockStateSets();
        // processedStateSet[blockStateSet.BlockHash].Processed.ShouldBeFalse();
        //
        // await blockStateSetsGrain.SetBlockStateSetProcessed(blockStateSet.BlockHash);
        // processedStateSet = await blockStateSetsGrain.GetBlockStateSets();
        // processedStateSet[blockStateSet.BlockHash].Processed.ShouldBeTrue();
        //
        // await blockStateSetsGrain.SetLongestChainBlockStateSet("NotExist");
        // var longestStateSet = await blockStateSetsGrain.GetLongestChainBlockStateSet();
        // longestStateSet.ShouldBeNull();
        //
        // await blockStateSetsGrain.SetLongestChainBlockStateSet(blockStateSet.BlockHash);
        // longestStateSet = await blockStateSetsGrain.GetLongestChainBlockStateSet();
        // longestStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
        //
        // await blockStateSetsGrain.SetCurrentBlockStateSet(blockStateSet);
        // var currentStateSet = await blockStateSetsGrain.GetCurrentBlockStateSet();
        // currentStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
        //
        // await blockStateSetsGrain.SetBestChainBlockStateSet("NotExist");
        // var bestStateSet = await blockStateSetsGrain.GetBestChainBlockStateSet();
        // bestStateSet.ShouldBeNull();
        //
        // await blockStateSetsGrain.SetBestChainBlockStateSet(blockStateSet.BlockHash);
        // bestStateSet = await blockStateSetsGrain.GetBestChainBlockStateSet();
        // bestStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
        //
        // await blockStateSetsGrain.SetLongestChainHashes(new Dictionary<string, string>
        //     { { blockStateSet.BlockHash, blockStateSet.PreviousBlockHash } });
        // var longestChainHashes = await blockStateSetsGrain.GetLongestChainHashes();
        // longestChainHashes.Count.ShouldBe(1);
        // longestChainHashes[blockStateSet.BlockHash].ShouldBe(blockStateSet.PreviousBlockHash);
        // stateSets = await blockStateSetsGrain.GetBlockStateSets();
        // stateSets[blockStateSet.BlockHash].Changes.Count.ShouldBe(1);
        //
        // await blockStateSetsGrain.SetLongestChainHashes(new Dictionary<string, string>
        //     { { blockStateSet.BlockHash, blockStateSet.PreviousBlockHash } }, true);
        // longestChainHashes = await blockStateSetsGrain.GetLongestChainHashes();
        // longestChainHashes.Count.ShouldBe(1);
        // longestChainHashes[blockStateSet.BlockHash].ShouldBe(blockStateSet.PreviousBlockHash);
        // stateSets = await blockStateSetsGrain.GetBlockStateSets();
        // stateSets[blockStateSet.BlockHash].Changes.Count.ShouldBe(0);
    }
}