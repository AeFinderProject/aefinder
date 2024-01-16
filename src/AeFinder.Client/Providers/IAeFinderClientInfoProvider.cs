namespace AeFinder.Client.Providers;

public interface IAeFinderClientInfoProvider
{
    string GetClientId();

    void SetClientId(string clientId);

    string GetVersion();

    void SetVersion(string version);
}