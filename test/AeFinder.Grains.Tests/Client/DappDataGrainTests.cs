using System.Threading.Tasks;
using AeFinder.Grains.Grain.Client;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.Client;

[Collection(ClusterCollection.Name)]
public class DappDataGrainTests: AeFinderGrainTestBase
{
    [Fact]
    public async Task DappData_Test()
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
}

public class DappData
{
    public int Value { get; set; }
}