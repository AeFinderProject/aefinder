using System;
using AeFinder.Block;
using AeFinder.Grains.BlockScan;
using AeFinder.Grains.Grain.BlockScan;
using AeFinder.Grains.Grain.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Grains;

public class ClusterFixture:IDisposable,ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }

    private class TestSiloConfigurations : ISiloBuilderConfigurator 
    {
        public void Configure(ISiloHostBuilder hostBuilder) {
            hostBuilder.ConfigureServices(services => {
                    services.AddSingleton<IBlockAppService, MockBlockAppService>();
                    services.AddSingleton<IBlockDataProvider, BlockDataProvider>();
                    services.AddTransient<IBlockFilterProvider, BlockFilterProvider>();
                    services.AddTransient<IBlockFilterProvider, TransactionFilterProvider>();
                    services.AddTransient<IBlockFilterProvider, LogEventFilterProvider>();
                    services.Configure<BlockScanOptions>(o =>
                    {
                        o.BatchPushBlockCount = 10;
                        o.ScanHistoryBlockThreshold = 5;
                        o.BatchPushNewBlockCount = 2;
                        o.MaxHistoricalBlockPushThreshold = 30;
                        o.MaxNewBlockPushThreshold = 30;
                    });
                    services.Configure<ClientOptions>(o =>
                    {
                        o.MaxCountPerBlockStateSetBucket = 5;
                    });
                })
                .AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }
    
    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName);
    }
}