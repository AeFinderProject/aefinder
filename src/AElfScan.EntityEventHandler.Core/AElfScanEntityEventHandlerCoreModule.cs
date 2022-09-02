using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AElfScanApplicationModule),
    typeof(AElfScanApplicationContractsModule))]
public class AElfScanEntityEventHandlerCoreModule:AbpModule
{
}