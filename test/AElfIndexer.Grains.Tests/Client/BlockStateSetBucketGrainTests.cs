using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Client;

[Collection(ClusterCollection.Name)]
public class BlockStateSetBucketGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task BlockStateSets_Test()
    {
        var blockStateSetGrain =
            Cluster.Client.GetGrain<IBlockStateSetBucketGrain<int>>("id");

        var version = Guid.NewGuid().ToString("N");
        var blockHash = "BlockHash102";
        
        var sets = await blockStateSetGrain.GetBlockStateSetsAsync(version);
        sets.Count.ShouldBe(0);

        var set = await blockStateSetGrain.GetBlockStateSetAsync(version, blockHash);
        set.ShouldBeNull();

        var blockSets = new Dictionary<string, BlockStateSet<int>>();
        for (int i = 0; i < 10; i++)
        {
            var hash = "BlockHash" + (i + 101);
            blockSets.Add(hash,new BlockStateSet<int>
            {
                BlockHash = hash,
                BlockHeight = 100 + i + 1,
                PreviousBlockHash = "BlockHash" + (i + 100),
                Confirmed = false,
                Data = new List<int>(),
                Changes = new Dictionary<string, string> { { "key", "value" } }
            });
        }
        await blockStateSetGrain.SetBlockStateSetsAsync(version, blockSets);
        
        sets = await blockStateSetGrain.GetBlockStateSetsAsync(version);
        sets.Count.ShouldBe(10);

        set = await blockStateSetGrain.GetBlockStateSetAsync(version, blockHash);
        set.BlockHash.ShouldBe(blockHash);

        var newVersion = Guid.NewGuid().ToString("N");
        blockSets = new Dictionary<string, BlockStateSet<int>>();
        for (int i = 0; i < 5; i++)
        {
            var hash = "BlockHash" + (i + 201);
            blockSets.Add(hash,new BlockStateSet<int>
            {
                BlockHash = hash,
                BlockHeight = 100 + i + 1,
                PreviousBlockHash = "BlockHash" + (i + 200),
                Confirmed = false,
                Data = new List<int>(),
                Changes = new Dictionary<string, string> { { "key", "value" } }
            });
        }
        await blockStateSetGrain.SetBlockStateSetsAsync(newVersion, blockSets);
        
        sets = await blockStateSetGrain.GetBlockStateSetsAsync(newVersion);
        sets.Count.ShouldBe(5);
        
        set = await blockStateSetGrain.GetBlockStateSetAsync(newVersion, blockHash);
        set.ShouldBeNull();

        await blockStateSetGrain.CleanAsync(newVersion);
        
        sets = await blockStateSetGrain.GetBlockStateSetsAsync(version);
        sets.Count.ShouldBe(0);
    }
}