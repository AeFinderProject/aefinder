using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace AeFinder.App.TestBase;

public class ClusterFixture
{
    public TestCluster Cluster { get; private set; }

    private const string MessageStreamName = "AeFinder";
    private const string GrainStorageName = "AeFinder";
    
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }
    
    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services => { })
                .AddMemoryStreams(MessageStreamName)
                .AddMemoryGrainStorage(GrainStorageName)
                .AddMemoryGrainStorageAsDefault();
        }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) =>
            clientBuilder.AddMemoryStreams(MessageStreamName);
    }
}