using AeFinder.Studio;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IUserAppGrain : IGrainWithStringKey
{
    Task AddApp(string appId, AeFinderAppInfo aeFinderAppInfo);

    Task AddApps(Dictionary<string, AeFinderAppInfo> apps);
    Task<Dictionary<string, AeFinderAppInfo>> GetApps();
}

public class UserAppGrain : Grain<UserAppGrainState>, IUserAppGrain
{
    public async Task AddApp(string appId, AeFinderAppInfo aeFinderAppInfo)
    {
        if (State.NameToApps.ContainsKey(appId))
        {
            return;
        }

        State.NameToApps[appId] = aeFinderAppInfo;
        await WriteStateAsync();
    }

    public Task AddApps(Dictionary<string, AeFinderAppInfo> apps)
    {
        foreach (var (key, value) in apps)
        {
            State.NameToApps[key] = value;
        }

        return WriteStateAsync();
    }

    public Task<Dictionary<string, AeFinderAppInfo>> GetApps()
    {
        return Task.FromResult(State.NameToApps);
    }
}