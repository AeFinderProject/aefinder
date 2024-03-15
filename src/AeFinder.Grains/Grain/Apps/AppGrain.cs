using AeFinder.Grains.State.Apps;
using AeFinder.Studio;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public class AppGrain : Grain<AppGrainState>, IAppGrain
{
    public async Task<ExistDto> Registe(string adminId, string appId, string name)
    {
        if (string.IsNullOrWhiteSpace(State.AdminId))
        {
            State.AdminId = adminId;
            State.AppId = appId;
            State.Name = name;
            await WriteStateAsync();
            return new ExistDto() { Exists = true, Added = true };
        }

        return new ExistDto() { Exists = State.AdminId == adminId, Added = false };
    }

    public async Task<AppInfo> AddDeveloperToApp(string developerId)
    {
        State.DeveloperIds.Add(developerId);
        await WriteStateAsync();
        return new AppInfo() { NameToApps = State.NameToApps };
    }

    public Task<bool> IsDeveloper(string developerId)
    {
        return Task.FromResult(State.DeveloperIds.Contains(developerId));
    }

    public async Task<AppInfo> AddOrUpdateAppByName(AeFinderAppInfo aeFinderAppInfo)
    {
        if (State.NameToApps.TryGetValue(aeFinderAppInfo.Name, out _))
        {
            State.NameToApps[aeFinderAppInfo.Name] = aeFinderAppInfo;
        }
        else
        {
            State.NameToApps.Add(aeFinderAppInfo.Name, aeFinderAppInfo);
        }

        await WriteStateAsync();
        return new AppInfo() { DeveloperIds = State.DeveloperIds };
    }

    public async Task<bool> AddAppName(string name)
    {
        if (State.NameToApps.ContainsKey(name))
        {
            return false;
        }

        State.NameToApps.Add(name, new AeFinderAppInfo());
        await WriteStateAsync();
        return true;
    }

    public Task<AeFinderAppInfo> GetAppByName(string appName)
    {
        return Task.FromResult(State.NameToApps.TryGetValue(appName, out var app) ? app : null);
    }
}