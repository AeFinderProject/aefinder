using AElfScan.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanEntityFrameworkCoreTestModule)
    )]
public class AElfScanDomainTestModule : AbpModule
{

}
