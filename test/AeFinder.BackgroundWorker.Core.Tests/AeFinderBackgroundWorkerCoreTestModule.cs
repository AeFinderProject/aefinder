using AeFinder.BackgroundWorker.Core;
using AeFinder.Orleans.TestBase;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AeFinder.BackgroundWorker.Tests;

[DependsOn(typeof(AeFinderBackGroundCoreModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule)
)]
public class AeFinderBackgroundWorkerCoreTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        
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