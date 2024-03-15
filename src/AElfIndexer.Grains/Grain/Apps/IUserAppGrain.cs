using AElfIndexer.Studio;
using Orleans;

namespace AElfIndexer.Grains.Grain.Apps;

public interface IUserAppGrain : IGrainWithStringKey
{
    Task<bool> AddOrUpdateAppByName(AeFinderAppInfo aeFinderAppInfo);
    Task<bool> AddAppName(string name);
    Task<AeFinderAppInfo> GetAppByName(string appName);
}

public class UserAppGrain : Grain<UserAppGrainState>, IUserAppGrain
{
    public async Task<bool> AddOrUpdateAppByName(AeFinderAppInfo aeFinderAppInfo)
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
        return true;
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