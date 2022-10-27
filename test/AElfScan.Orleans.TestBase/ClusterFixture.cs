using AElf.Orleans.EventSourcing.Snapshot;
using AElfScan.Grain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using AElf.Orleans.EventSourcing.Snapshot.Hosting;
using EventStore.ClientAPI;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Orleans.TestBase;

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
                .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.DataConnectionString = "localhost:6666"; // This is the deafult
                    options.UseJson = true;
                    options.DatabaseNumber = 1;
                }))
                // .AddMemoryGrainStorageAsDefault()
                .AddSnapshotStorageBasedLogConsistencyProviderAsDefault((op, name) => 
                {
                    // op.UseIndependentEventStorage = false;
                    op.UseIndependentEventStorage = true;
                    // Should configure event storage when set UseIndependentEventStorage true
                    op.ConfigureIndependentEventStorage = (services, name) =>
                    {
                        var eventStoreConnectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";
                        var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString);
                        eventStoreConnection.ConnectAsync().Wait();
                
                        services.AddSingleton(eventStoreConnection);
                        services.AddSingleton<IGrainEventStorage, EventStoreGrainEventStorage>();
                    };
                });
        }
    }
}