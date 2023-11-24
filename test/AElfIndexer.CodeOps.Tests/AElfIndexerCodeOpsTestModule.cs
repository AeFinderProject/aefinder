using Volo.Abp.Modularity;

namespace AElfIndexer.CodeOps.Tests;

[DependsOn(
    typeof(AElfIndexerCodeOpsModule)
)]
public class AElfIndexerCodeOpsTestModule : AbpModule
{
    
}