using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace AElfIndexer.MongoDB;

[DependsOn(
    typeof(AElfIndexerTestBaseModule),
    typeof(AElfIndexerMongoDbModule)
    )]
public class AElfIndexerMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var stringArray = AElfIndexerMongoDbFixture.ConnectionString.Split('?');
        var connectionString = stringArray[0].EnsureEndsWith('/') +
                                   "Db_" +
                               Guid.NewGuid().ToString("N") + "/?" + stringArray[1];

        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = connectionString;
        });
    }
}
