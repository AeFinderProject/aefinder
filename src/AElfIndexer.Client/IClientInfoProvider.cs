namespace AElfIndexer.Client;

public interface IClientInfoProvider<T>
{
    string GetClientId();

    void SetClientId(string clientId);

    Dictionary<string, string> GetIndexPrefixes();

    void SetIndexPrefixes(Dictionary<string, string> indexPrefixes);
}