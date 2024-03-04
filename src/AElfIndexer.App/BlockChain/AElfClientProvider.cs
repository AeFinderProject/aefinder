using AElf.Client;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.BlockChain;

public class AElfClientProvider : IAElfClientProvider, ISingletonDependency
{
    private readonly Dictionary<string, AElfClient> _clients = new();

    public AElfClientProvider(IOptionsSnapshot<ChainNodeOptions> nodeOptions)
    {
        var clientBuilder = new AElfClientBuilder();
        foreach (var node in nodeOptions.Value.ChainNodes)
        {
            _clients[node.Key] = clientBuilder.UseEndpoint(node.Value).Build();
        }
    }

    public AElfClient GetClient(string chainId)
    {
        return _clients[chainId];
    }
}