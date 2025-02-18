using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Grains.Grain.Apps;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.Apps;

[Collection(ClusterCollection.Name)]
public class AppGrainTests : AeFinderGrainTestBase
{
    [Fact]
    public async Task DeleteApp_Test()
    {
        var orgId = Guid.NewGuid();
        var createInput = new CreateAppDto
        {
            AppId = "appid",
            OrganizationId = orgId.ToString("N"),
            AppName = "app"
        };

        var appGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(createInput.AppId));
        var app = await appGrain.CreateAsync(createInput);
        app.AppId.ShouldBe(createInput.AppId);
        
        var organizationAppGain =
            Cluster.Client.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GenerateOrganizationAppGrainId(createInput.OrganizationId));
        var appIds = await organizationAppGain.GetAppsAsync();
        appIds.Count.ShouldBe(1);
        appIds.ShouldContain(createInput.AppId);

        await appGrain.DeleteAppAsync();
        app = await appGrain.GetAsync();
        app.Status.ShouldBe(AppStatus.Deleted);
        
        appIds = await organizationAppGain.GetAppsAsync();
        appIds.Count.ShouldBe(0);
    }
}