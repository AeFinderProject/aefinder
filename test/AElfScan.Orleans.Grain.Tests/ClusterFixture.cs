using System;
using AElf.Orleans.EventSourcing.Snapshot;
using AElfScan.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using AElf.Orleans.EventSourcing.Snapshot.Hosting;
using AElfScan.AElf;
using AElfScan.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;

namespace AElfScan;

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

    private class TestSiloConfigurations : ISiloBuilderConfigurator {
        public void Configure(ISiloHostBuilder hostBuilder) {
            hostBuilder.ConfigureServices(services => {
                    //services.AddApplication<AElfScanGrainTestModule>();
                    services.AddSingleton<IBlockAppService, MockBlockAppService>();
                    services.AddSingleton<IBlockDataProvider, BlockDataProvider>();
                    services.AddTransient<IBlockFilterProvider, BlockFilterProvider>();
                    services.AddTransient<IBlockFilterProvider, TransactionFilterProvider>();
                    services.AddTransient<IBlockFilterProvider, LogEventFilterProvider>();
                    services.Configure<BlockScanOptions>(o =>
                    {
                        o.BatchPushBlockCount = 10;
                        o.ScanHistoryBlockThreshold = 5;
                    });
                })
                .AddSimpleMessageStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }
    
    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddSimpleMessageStreamProvider(AElfScanApplicationConsts.MessageStreamName)
        ;
        
    }
}