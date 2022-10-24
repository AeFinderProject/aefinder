using AElfScan.Grain.Contracts;
using Volo.Abp.Modularity;

namespace AElfScan.Grain;

[DependsOn(typeof(AElfScanGrainContractsModule))]
public class AElfScanGrainModule: AbpModule
{
    
}