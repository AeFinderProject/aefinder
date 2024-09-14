using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.Grains.Grain.Apps;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.Subscriptions;

[Collection(ClusterCollection.Name)]
public class AppResourceLimitGrainTests : AeFinderGrainTestBase
{
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;
    
    public AppResourceLimitGrainTests()
    {
        _appResourceLimitProvider = GetRequiredService<IAppResourceLimitProvider>();
    }

    [Fact]
    public async Task SetAppOperationLimit_Test()
    {
        var appId = "AppId";
        var appResourceLimitGrain = Cluster.Client.GetGrain<IAppResourceLimitGrain>(GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var limitDto = new SetAppResourceLimitDto()
        {
            MaxEntityCallCount = 555
        };
        await appResourceLimitGrain.SetAsync(limitDto);
        var limitGrainInfo = await appResourceLimitGrain.GetAsync();
        limitGrainInfo.MaxEntitySize.ShouldBe(0);
        limitGrainInfo.MaxEntityCallCount.ShouldBe(555);

        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        resourceLimitInfo.MaxEntitySize.ShouldBe(99999);
        resourceLimitInfo.MaxEntityCallCount.ShouldBe(555);
    }

    [Fact]
    public async Task SetAppKubernetesLimit_Test()
    {
        var appId = "AppId";
        var appResourceLimitGrain = Cluster.Client.GetGrain<IAppResourceLimitGrain>(GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var limitDto = new SetAppResourceLimitDto()
        {
            AppPodReplicas = 3,
            AppQueryPodRequestMemory="1.5Gi"
        };
        await appResourceLimitGrain.SetAsync(limitDto);
        var limitGrainInfo = await appResourceLimitGrain.GetAsync();
        limitGrainInfo.AppFullPodRequestCpuCore.ShouldBeNull();
        limitGrainInfo.AppQueryPodRequestMemory.ShouldBe("1.5Gi");
        limitGrainInfo.AppPodReplicas.ShouldBe(3);
        
        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        resourceLimitInfo.AppFullPodRequestCpuCore.ShouldBe("4");
        resourceLimitInfo.MaxEntityCallCount.ShouldBe(200);
        resourceLimitInfo.AppQueryPodRequestMemory.ShouldBe("1.5Gi");
        resourceLimitInfo.AppPodReplicas.ShouldBe(3);
    }

    [Fact]
    public async Task SetDeployLimit_Test()
    {
        var appId = "AppId";
        var appResourceLimitGrain = Cluster.Client.GetGrain<IAppResourceLimitGrain>(GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var limitDto = new SetAppResourceLimitDto()
        {
            MaxAppAttachmentSize = 10240000
        };
        await appResourceLimitGrain.SetAsync(limitDto);
        
        var limitGrainInfo = await appResourceLimitGrain.GetAsync();
        limitGrainInfo.MaxAppCodeSize.ShouldBe(0);
        limitGrainInfo.MaxAppAttachmentSize.ShouldBe(10240000);
        
        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        resourceLimitInfo.MaxAppCodeSize.ShouldBe(2048);
        resourceLimitInfo.MaxAppAttachmentSize.ShouldBe(10240000);
    }
}