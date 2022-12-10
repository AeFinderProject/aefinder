namespace AElfIndexer.Client.Providers;

public interface IAElfIndexerClientInfoProvider<T>
{
    string GetClientId();

    void SetClientId(string clientId);

    string GetIndexPrefix();

    void SetIndexPrefix(string prefix);
}