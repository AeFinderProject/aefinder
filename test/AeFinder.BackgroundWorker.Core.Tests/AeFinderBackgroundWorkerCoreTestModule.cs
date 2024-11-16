using AeFinder.BackgroundWorker.Core;
using AeFinder.Metrics;
using AeFinder.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AeFinder.BackgroundWorker.Tests;

[DependsOn(typeof(AeFinderBackGroundCoreModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainTestModule)
)]
public class AeFinderBackgroundWorkerCoreTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IKubernetesAppMonitor, DefaultKubernetesAppMonitor>();
    }
    //
    // public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    // {
    //     
    // }
    //
    // public override void OnApplicationShutdown(ApplicationShutdownContext context)
    // {
    //     
    // }
}