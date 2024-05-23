using Microsoft.Extensions.DependencyInjection;
using AElf.Orleans.EventSourcing.Snapshot.Hosting;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Blocks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

namespace AeFinder.Orleans.TestBase;

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
                    services.AddSingleton<IBlockGrain, BlockGrain>();
                    services.AddTransient(p=>Mock.Of<IBlockFilterProvider>());
                    services.AddAutoMapper(typeof(AeFinderApplicationModule).Assembly);
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        onServiceExposingContext.ExposedTypes.AddRange(
                            ReflectionHelper.GetImplementedGenericTypes(
                                onServiceExposingContext.ImplementationType,
                                typeof(IObjectMapper<,>)
                            )
                        );
                    });
                    services.AddTransient(
                        typeof(IObjectMapper<>),
                        typeof(DefaultObjectMapper<>)
                    );
                    services.AddTransient(
                        typeof(IObjectMapper),
                        typeof(DefaultObjectMapper)
                    );
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                    // services.AddTransient<IDistributedEventBus, LocalDistributedEventBus>();
                    // services.AddTransient<ILocalEventBus, LocalEventBus>();
                    var mockDistributedEventBus = new Mock<IDistributedEventBus>();
                    // mock IDistributedEventBus
                    services.AddSingleton<IDistributedEventBus>(mockDistributedEventBus.Object);
                })
                // .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                // {
                //     options.DataConnectionString = "localhost:6379"; // This is the deafult
                //     options.UseJson = true;
                //     options.DatabaseNumber = 0;
                // }))
                .AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault()
                .AddSnapshotStorageBasedLogConsistencyProviderAsDefault((op, name) => 
                {
                    op.UseIndependentEventStorage = false;
                    // op.UseIndependentEventStorage = true;
                    // // Should configure event storage when set UseIndependentEventStorage true
                    // op.ConfigureIndependentEventStorage = (services, name) =>
                    // {
                    //     var eventStoreConnectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";
                    //     var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString);
                    //     eventStoreConnection.ConnectAsync().Wait();
                    //
                    //     services.AddSingleton(eventStoreConnection);
                    //     services.AddSingleton<IGrainEventStorage, EventStoreGrainEventStorage>();
                    // };
                });
        }
    }
    
    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName);
    }
}

public class MapperAccessor : IMapperAccessor
{
    public IMapper Mapper { get; set; }
}