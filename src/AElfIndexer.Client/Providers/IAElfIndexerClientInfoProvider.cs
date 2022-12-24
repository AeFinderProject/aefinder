namespace AElfIndexer.Client.Providers;

public interface IAElfIndexerClientInfoProvider
{
    string GetClientId();

    void SetClientId(string clientId);

    string GetVersion();

    void SetVersion(string version);
}