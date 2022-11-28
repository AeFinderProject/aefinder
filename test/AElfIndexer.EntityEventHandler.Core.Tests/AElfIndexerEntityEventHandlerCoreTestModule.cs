using System.Runtime.CompilerServices;
using AElfIndexer.Grains;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AElfIndexerEntityEventHandlerCoreModule),
    typeof(AElfIndexerOrleansTestBaseModule))]
public class AElfIndexerEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfIndexerEntityEventHandlerCoreTestModule>();
        });

        
    }
}