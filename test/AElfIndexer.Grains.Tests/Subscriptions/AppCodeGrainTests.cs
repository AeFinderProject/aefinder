using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Subscriptions;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Subscriptions;

[Collection(ClusterCollection.Name)]
public class AppCodeGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task SetCodeTest()
    {
        var appId = "AppId";
        var version = "Version";
        var code = new byte[] {1, 2, 3, 4, 5};
        var grainId = GrainIdHelper.GenerateGetAppCodeGrainId(appId, version);
        var grain = Cluster.Client.GetGrain<IAppCodeGrain>(grainId);
        await grain.SetCodeAsync(code);
        
        var result = await grain.GetCodeAsync();
        Assert.Equal(code, result);

        await grain.RemoveAsync();
        
        result = await grain.GetCodeAsync(); 
        result.ShouldBeNull();
    }
}