using AeFinder.MongoDb;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AeFinder.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationContractsModule)
    )]
public class AeFinderDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
