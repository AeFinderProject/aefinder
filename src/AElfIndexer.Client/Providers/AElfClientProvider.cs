using AElf.Client;
using Microsoft.Extensions.Options;

namespace AElfIndexer.Client.Providers;

public class AElfClientProvider : IAElfClientProvider
{
    private Dictionary<string, AElfClient> _clients = new();
    public AElfClientProvider(IOptionsSnapshot<NodeOptions> nodeOptions)
    {
        var clientBuilder = new AElfClientBuilder();
        foreach (var config in nodeOptions.Value.NodeConfigList)
        {
            _clients[config.ChainId] = clientBuilder.UseEndpoint(config.Endpoint).Build();
        }
    }

    public AElfClient GetClient(string chainId)
    {
        return _clients[chainId];
    }

    public bool TryAddClient(string chainId, string endpoint)
    {
        return _clients.TryAdd(chainId, new AElfClient(endpoint));
    }

    public void RemoveClient(string chainId)
    {
        _clients.Remove(chainId);
    }
}