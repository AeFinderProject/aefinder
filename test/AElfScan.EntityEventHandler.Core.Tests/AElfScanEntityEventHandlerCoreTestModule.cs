using Volo.Abp.Modularity;

namespace AElfScan.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AElfScanEntityEventHandlerCoreModule))]
public class AElfScanEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        
    }
}