using AeFinder.BlockScan;
using AeFinder.Grains.Grain.Blocks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;
using Moq;
using Orleans.Serialization;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;
using Volo.Abp.Timing;

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
    
    private class TestSiloConfigurations : ISiloConfigurator 
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton<IBlockGrain, BlockGrain>();
                    services.AddTransient(p => Mock.Of<IBlockFilterAppService>());
                    services.AddAutoMapper(typeof(AeFinderApplicationModule).Assembly);
                    services.AddTransient<IClock, Clock>();
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
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
                    // services.AddTransient<IDistributedEventBus, LocalDistributedEventBus>();
                    // services.AddTransient<ILocalEventBus, LocalEventBus>();
                    var mockDistributedEventBus = new Mock<IDistributedEventBus>();
                    // mock IDistributedEventBus
                    services.AddSingleton<IDistributedEventBus>(mockDistributedEventBus.Object);
        
                    services.Configure<ExceptionSerializationOptions>(options =>
                        options.SupportedNamespacePrefixes.Add("Volo.Abp"));
                })
                .AddMemoryStreams(AeFinderApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }
    
    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddMemoryStreams(AeFinderApplicationConsts.MessageStreamName);
    }
}

public class MapperAccessor : IMapperAccessor
{
    public IMapper Mapper { get; set; }
}