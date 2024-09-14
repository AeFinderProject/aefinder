using AeFinder.MongoDb;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.BackgroundWorker.Core;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AeFinderApplicationContractsModule))]
public class AeFinderBackGroundCoreModule:AbpModule
{
}