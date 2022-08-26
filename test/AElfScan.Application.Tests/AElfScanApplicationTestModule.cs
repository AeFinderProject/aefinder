using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanApplicationModule),
    typeof(AElfScanDomainTestModule)
    )]
public class AElfScanApplicationTestModule : AbpModule
{

}
