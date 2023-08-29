using System.Runtime.CompilerServices;
using AElfIndexer.Block;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AElfIndexerEntityEventHandlerCoreTestModule))]
public class EntityEventHandlerCoreBlockIndexTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockAppService, MockBlockAppService>();
    }
}