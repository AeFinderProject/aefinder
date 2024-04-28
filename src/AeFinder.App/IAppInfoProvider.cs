using Volo.Abp.DependencyInjection;

namespace AeFinder.App;

public interface IAppInfoProvider
{
    public string AppId { get; }
    public string Version { get; }
    
    void SetAppId(string appId);
    void SetVersion(string version);
}

public class AppInfoProvider : IAppInfoProvider, ISingletonDependency
{
    public string AppId { get; private set; }
    public string Version { get; private set;}
    
    public void SetAppId(string appId)
    {
        AppId = appId;
    }

    public void SetVersion(string version)
    {
        Version = version;
    }
}