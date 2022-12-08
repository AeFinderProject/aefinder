using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client;

public class ClientInfoProvider<T> : IClientInfoProvider<T>, ISingletonDependency
{
    private string _clientId;
    // chainId => IndexPrefix
    private Dictionary<string, string> _indexPrefixes;


    public string GetClientId()
    {
        return _clientId;
    }

    public void SetClientId(string clientId)
    {
        _clientId = clientId;
    }

    public Dictionary<string, string> GetIndexPrefixes()
    {
        return _indexPrefixes;
    }

    public void SetIndexPrefixes(Dictionary<string, string> indexPrefixes)
    {
        _indexPrefixes = indexPrefixes;
    }
}