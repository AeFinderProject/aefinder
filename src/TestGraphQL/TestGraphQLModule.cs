using AElfIndexer;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace GraphQL;

[DependsOn(typeof(AElfIndexerApplicationModule), typeof(AElfIndexerClientModule))]
public class TestGraphQLModule : AElfIndexerClientPluginBaseModule<TestGraphQLModule, TestSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IBlockService, BlockService>();
        serviceCollection.AddTransient<IBlockChainDataHandler, BlockHandler>();
    }

    protected override string ClientId => "AElfIndexer_DApp";
    protected override string Version => "107f2a0661434f5aa9bb27de04e61df1";
}

