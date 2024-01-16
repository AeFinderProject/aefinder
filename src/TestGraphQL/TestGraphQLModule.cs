using AeFinder;
using AeFinder.Client;
using AeFinder.Client.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace GraphQL;

[DependsOn(typeof(AeFinderApplicationModule), typeof(AeFinderClientModule))]
public class TestGraphQLModule : AeFinderClientPluginBaseModule<TestGraphQLModule, TestSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IBlockService, BlockService>();
        serviceCollection.AddTransient<IBlockChainDataHandler, BlockHandler>();
    }

    protected override string ClientId => "AeFinder_DApp";
    protected override string Version => "107f2a0661434f5aa9bb27de04e61df1";
}

