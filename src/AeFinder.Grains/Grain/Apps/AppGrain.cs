using AeFinder.Grains.State.Apps;
using AeFinder.Studio;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public class AppGrain : Grain<AppGrainState>, IAppGrain
{
    public async Task<RegisterDto> Register(string adminId, string appId, string name)
    {
        if (!string.IsNullOrWhiteSpace(State.AdminId))
        {
            return new RegisterDto() { Success = State.AdminId == adminId && State.Name.Equals(name), Added = false };
        }

        State.AdminId = adminId;
        State.AppId = appId;
        State.Name = name;
        State.AeFinderAppInfo = new AeFinderAppInfo() { AppId = appId, Name = name };
        await WriteStateAsync();
        return new RegisterDto() { Success = true, Added = true };
    }

    public async Task<AppInfo> AddDeveloperToApp(string developerId)
    {
        var appInfo = new AppInfo() { AeFinderAppInfo = State.AeFinderAppInfo, DeveloperIds = State.DeveloperIds, AppId = State.AppId };
        if (developerId.IsNullOrEmpty())
        {
            return appInfo;
        }

        State.DeveloperIds.Add(developerId);
        await WriteStateAsync();
        return appInfo;
    }

    public Task<bool> IsDeveloper(string developerId)
    {
        return Task.FromResult(State.DeveloperIds.Contains(developerId));
    }

    public async Task<AppInfo> AddOrUpdateAppInfo(AeFinderAppInfo aeFinderAppInfo)
    {
        if (State.AppId.IsNullOrEmpty())
        {
            return null;
        }

        aeFinderAppInfo.AppId = State.AppId;
        aeFinderAppInfo.Name = State.AppId;
        State.AeFinderAppInfo = aeFinderAppInfo;

        await WriteStateAsync();
        return new AppInfo() { AeFinderAppInfo = State.AeFinderAppInfo, DeveloperIds = State.DeveloperIds, AppId = State.AppId };
    }


    public Task<AeFinderAppInfo> GetAppInfo()
    {
        return Task.FromResult(State.AeFinderAppInfo);
    }

    public Task<bool> IsAdmin(string appId)
    {
        return Task.FromResult(!appId.IsNullOrEmpty() && State.AppId.IsNullOrEmpty() && appId.Equals(State.AdminId));
    }

    public async Task SetGraphQlByVersion(string version, string graphQl)
    {
        State.VersionToGraphQl[version] = graphQl;
        await WriteStateAsync();
    }

    public Task<string> GetGraphQlByVersion(string version)
    {
        return Task.FromResult(State.VersionToGraphQl.TryGetValue(version, out var value) ? value : null);
    }

    public Task<Dictionary<string, string>> GetGraphQls()
    {
        return Task.FromResult(State.VersionToGraphQl);
    }
}