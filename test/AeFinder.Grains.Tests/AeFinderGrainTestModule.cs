using System.Collections.Generic;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Block;
using AeFinder.Grains.BlockPush;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Options;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AeFinder.Grains;

[DependsOn(
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderGrainsModule),
    typeof(AeFinderApplicationModule)
    )]
public class AeFinderGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IBlockAppService, MockBlockAppService>();
        context.Services.Configure<OperationLimitOptions>(o =>
        {
            o.MaxContractCallCount = 30;
            o.MaxEntitySize = 99999;
            o.MaxEntityCallCount = 200;
            o.MaxLogSize = 10100;
            o.MaxLogCallCount = 13;
        });
        context.Services.Configure<KubernetesOptions>(o =>
        {
            o.AppFullPodRequestCpuCore = "4";
            o.AppFullPodRequestMemory = "8Gi";
            o.AppQueryPodRequestCpuCore = "2";
            o.AppQueryPodRequestMemory = "3Gi";
            o.AppPodReplicas = 5;
        });
        context.Services.Configure<AppDeployOptions>(o =>
        {
            o.MaxAppCodeSize = 2048;
            o.MaxAppAttachmentSize = 66666;
        });
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
    }
}
