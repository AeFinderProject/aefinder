using System;
using AElf.Orleans.EventSourcing.Snapshot;
using AElfScan.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using AElf.Orleans.EventSourcing.Snapshot.Hosting;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;

namespace AElfScan;

public class ClusterFixture:IDisposable,ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
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
                    services.AddSingleton<IBlockGrain, BlockGrain>();
                })
                .AddMemoryGrainStorageAsDefault();
        }
    }
}