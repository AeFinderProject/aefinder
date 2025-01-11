using AeFinder.BackgroundWorker.Core;
using AeFinder.Metrics;
using AeFinder.Orleans.TestBase;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AeFinder.BackgroundWorker.Tests;

[DependsOn(typeof(AeFinderBackGroundCoreModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainTestModule)
)]
public class AeFinderBackgroundWorkerCoreTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IKubernetesAppMonitor, DefaultKubernetesAppMonitor>();
        context.Services.AddHttpClient();
        var mockGraphQLClient = new Mock<IGraphQLClient>();
        context.Services.AddSingleton<IGraphQLClient>(mockGraphQLClient.Object);
    }
    //
    // public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    // {
    //     
    // }
    //
    // public override void OnApplicationShutdown(ApplicationShutdownContext context)
    // {
    //     
    // }
}