using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Client;

[Collection(ClusterCollection.Name)]
public class DappDataGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task DappDataTest()
    {
        var grain = Cluster.Client.GetGrain<IDappDataGrain>("id");
        var latestValue = "latestValue";
        var libValue = "libValue";

        await grain.SetLIBValue(libValue);

        var latest = await grain.GetLatestValue();
        latest.ShouldBe(libValue);
        var lib = await grain.GetLIBValue();
        lib.ShouldBe(libValue);
        var dappData = await grain.GetValue();
        dappData.LatestValue.ShouldBe(libValue);
        dappData.LIBValue.ShouldBe(libValue);
        
        await grain.SetLatestValue(latestValue);
        latest = await grain.GetLatestValue();
        latest.ShouldBe(latestValue);
        lib = await grain.GetLIBValue();
        lib.ShouldBe(libValue);
        dappData = await grain.GetValue();
        dappData.LatestValue.ShouldBe(latestValue);
        dappData.LIBValue.ShouldBe(libValue);
    }
    
    [Fact]
    public async Task DappDataJsonTest()
    {
        var grain = Cluster.Client.GetGrain<IDappDataGrain>("id");
        var latestValue = new DappData { Value = 100 };
        var libValue = new DappData { Value = 90 };

        await grain.SetLIBValue<DappData>(libValue);

        var latest = await grain.GetLatestValue<DappData>();
        latest.Value.ShouldBe(libValue.Value);
        var lib = await grain.GetLIBValue<DappData>();
        lib.Value.ShouldBe(libValue.Value);
        var dappData = await grain.GetValue<DappData>();
        dappData.LatestValue.Value.ShouldBe(libValue.Value);
        dappData.LIBValue.Value.ShouldBe(libValue.Value);
        
        await grain.SetLatestValue<DappData>(latestValue);
        latest = await grain.GetLatestValue<DappData>();
        latest.Value.ShouldBe(latestValue.Value);
        lib = await grain.GetLIBValue<DappData>();
        lib.Value.ShouldBe(libValue.Value);
        dappData = await grain.GetValue<DappData>();
        dappData.LatestValue.Value.ShouldBe(latestValue.Value);
        dappData.LIBValue.Value.ShouldBe(libValue.Value);
    }
}

public class DappData
{
    public int Value { get; set; }
}