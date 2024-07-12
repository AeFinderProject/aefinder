using System;
using AeFinder.Block;
using AeFinder.BlockScan;
using AeFinder.Grains.BlockPush;
using AeFinder.Grains.Grain.BlockPush;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.AutoMapper;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

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
                    services.AddTransient<IBlockFilterAppService, BlockFilterAppService>();
                    services.Configure<BlockPushOptions>(o =>
                    {
                        o.BatchPushBlockCount = 10;
                        o.PushHistoryBlockThreshold = 5;
                        o.BatchPushNewBlockCount = 2;
                        o.MaxHistoricalBlockPushThreshold = 30;
                        o.MaxNewBlockPushThreshold = 30;
                    });
                    services.AddAutoMapper(typeof(AeFinderApplicationModule).Assembly);
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        // onServiceExposingContext.ExposedTypes.AddRange(
                        //     ReflectionHelper.GetImplementedGenericTypes(
                        //         onServiceExposingContext.ImplementationType,
                        //         typeof(IObjectMapper<,>)
                        //     )
                        // );
                        var implementedTypes = ReflectionHelper.GetImplementedGenericTypes(
                            onServiceExposingContext.ImplementationType,
                            typeof(IObjectMapper<,>)
                        );

                        foreach (var type in implementedTypes)
                        {
                            onServiceExposingContext.ExposedTypes.Add(new ServiceIdentifier(type));
                        }
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
                    var mockDistributedEventBus = new Mock<IDistributedEventBus>();
                    // mock IDistributedEventBus
                    services.AddSingleton<IDistributedEventBus>(mockDistributedEventBus.Object);
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

public class MapperAccessor : IMapperAccessor
{
    public IMapper Mapper { get; set; }
}