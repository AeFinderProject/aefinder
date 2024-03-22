using AeFinder.Studio;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppGrain : IGrainWithStringKey
{
    Task<RegisterDto> Register(string adminId, string appId, string name);
    Task<AppInfo> AddDeveloperToApp(string developerId);
    Task<bool> IsDeveloper(string developerId);

    Task<AppInfo> AddOrUpdateAppInfo(AeFinderAppInfo aeFinderAppInfo);
    Task<AeFinderAppInfo> GetAppInfo();
    Task<bool> IsAdmin(string adminId);

    Task SetGraphQlByVersion(string version, string graphQl);

    Task<string> GetGraphQlByVersion(string version);
    Task<Dictionary<string, string>> GetGraphQls();
}