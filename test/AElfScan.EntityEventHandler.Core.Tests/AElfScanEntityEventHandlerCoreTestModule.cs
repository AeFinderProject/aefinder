using System.Runtime.CompilerServices;
using AElfScan.Grains;
using AElfScan.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScan.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AElfScanEntityEventHandlerCoreModule),
    typeof(AElfScanOrleansTestBaseModule))]
public class AElfScanEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfScanEntityEventHandlerCoreTestModule>();
        });

        
    }
}