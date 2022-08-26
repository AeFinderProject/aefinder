using AElfScan.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElfScan.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AElfScanEntityFrameworkCoreModule),
    typeof(AElfScanApplicationContractsModule)
    )]
public class AElfScanDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
