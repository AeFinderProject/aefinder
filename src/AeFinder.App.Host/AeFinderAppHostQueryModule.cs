using Volo.Abp.Modularity;

namespace AeFinder.App;

[DependsOn(typeof(AeFinderAppHostBaseModule))]
public class AeFinderAppHostQueryModule: AbpModule
{
    
}