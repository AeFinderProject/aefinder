using AeFinder.Studio;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppGrain : IGrainWithStringKey
{
    Task<ExistDto> Register(string adminId, string appId, string name);
    Task<AppInfo> AddDeveloperToApp(string developerId);
    Task<bool> IsDeveloper(string developerId);

    Task<AppInfo> AddOrUpdateAppByName(AeFinderAppInfo aeFinderAppInfo);
    Task<bool> AddAppName(string name);
    Task<AeFinderAppInfo> GetAppByName(string appName);
    Task<bool> IsAdmin(string adminId);
}