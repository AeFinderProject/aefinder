using AElfIndexer.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElfIndexer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerApplicationContractsModule)
    )]
public class AElfIndexerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
