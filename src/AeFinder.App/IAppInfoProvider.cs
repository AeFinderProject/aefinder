using Volo.Abp.DependencyInjection;

namespace AeFinder.App;

public interface IAppInfoProvider
{
    public string AppId { get; }
    public string Version { get; }
    public string ChainId { get; }

    void SetAppId(string appId);
    void SetVersion(string version);
    void SetChainId(string chainId);
}

public class AppInfoProvider : IAppInfoProvider, ISingletonDependency
{
    public string AppId { get; private set; }
    public string Version { get; private set;}
    public string ChainId { get; private set;}

    public void SetAppId(string appId)
    {
        AppId = appId;
    }

    public void SetVersion(string version)
    {
        Version = version;
    }

    public void SetChainId(string chainId)
    {
        ChainId = chainId;
    }
}