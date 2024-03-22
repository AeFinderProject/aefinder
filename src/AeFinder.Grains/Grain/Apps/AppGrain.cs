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
        await WriteStateAsync();
        return new RegisterDto() { Success = true, Added = true };
    }

    public async Task<AppInfo> AddDeveloperToApp(string developerId)
    {
        State.DeveloperIds.Add(developerId);
        await WriteStateAsync();
        return new AppInfo() { AeFinderAppInfo = State.AeFinderAppInfo };
    }

    public Task<bool> IsDeveloper(string developerId)
    {
        return Task.FromResult(State.DeveloperIds.Contains(developerId));
    }

    public async Task<AppInfo> AddOrUpdateAppInfo(AeFinderAppInfo aeFinderAppInfo)
    {
        if (State.AeFinderAppInfo == null || State.AeFinderAppInfo.Name.Equals(aeFinderAppInfo.Name))
        {
            State.AeFinderAppInfo = aeFinderAppInfo;
        }

        await WriteStateAsync();
        return new AppInfo() { DeveloperIds = State.DeveloperIds };
    }


    public Task<AeFinderAppInfo> GetAppInfo()
    {
        return Task.FromResult(State.AeFinderAppInfo);
    }

    public async Task<bool> IsAdmin(string adminId)
    {
        return !adminId.IsNullOrEmpty() && State.AdminId.IsNullOrEmpty() && adminId.Equals(State.AdminId);
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