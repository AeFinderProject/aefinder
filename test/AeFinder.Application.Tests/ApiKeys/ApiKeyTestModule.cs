using System;
using AeFinder.Grains;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Clusters;
using Orleans;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.ApiKeys;

[DependsOn(typeof(AeFinderApplicationTestModule))]
public class ApiKeyTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IApiQueryPriceProvider, MockApiQueryPriceProvider>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        
    }
}