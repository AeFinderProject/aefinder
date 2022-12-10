using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public class AElfIndexerClientInfoProvider<T> : IAElfIndexerClientInfoProvider<T>, ISingletonDependency
{
    private string _clientId;
    // chainId => IndexPrefix
    private string _indexPrefix;


    public string GetClientId()
    {
        return _clientId;
    }

    public void SetClientId(string clientId)
    {
        _clientId = clientId;
    }

    public string GetIndexPrefix()
    {
        return _indexPrefix;
    }

    public void SetIndexPrefix(string indexPrefix)
    {
        _indexPrefix = indexPrefix;
    }
}