using Volo.Abp.Modularity;

namespace AElfIndexer.App.Host;

[DependsOn(typeof(AElfIndexerAppHostBaseModule))]
public class AElfIndexerAppHostQueryModule: AbpModule
{
    
}