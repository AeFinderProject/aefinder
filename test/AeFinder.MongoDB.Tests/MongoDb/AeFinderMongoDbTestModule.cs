using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace AeFinder.MongoDb;

[DependsOn(
    typeof(AeFinderTestBaseModule),
    typeof(AeFinderMongoDbModule)
    )]
public class AeFinderMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var stringArray = AeFinderMongoDbFixture.ConnectionString.Split('?');
        var connectionString = stringArray[0].EnsureEndsWith('/') +
                                   "Db_" +
                               Guid.NewGuid().ToString("N") + "/?" + stringArray[1];

        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = connectionString;
        });
    }
}
