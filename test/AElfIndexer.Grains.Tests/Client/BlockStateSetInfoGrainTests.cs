using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Client;

[Collection(ClusterCollection.Name)]
public class BlockStateSetInfoGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task GetConfirmedBlockHeight_Test()
    {
        var grain = Cluster.Client.GetGrain<IBlockStateSetInfoGrain>("id");
        var height = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        height.ShouldBe(0);
        height = await grain.GetConfirmedBlockHeight(BlockFilterType.Transaction);
        height.ShouldBe(0);

        await grain.SetConfirmedBlockHeight(BlockFilterType.Block, 100);
        height = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        height.ShouldBe(100);
        height = await grain.GetConfirmedBlockHeight(BlockFilterType.Transaction);
        height.ShouldBe(0);
    }
}