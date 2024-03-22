using AeFinder.Grains.State.Apps;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppNameGrain : IGrainWithStringKey
{
    Task<bool> Register(string appId);
}

public class AppNameGrain : Grain<AppNameState>, IAppNameGrain
{
    public async Task<bool> Register(string appId)
    {
        if (appId == null)
        {
            return false;
        }

        if (State.AppId != null)
        {
            return State.AppId.Equals(appId);
        }

        State.AppId = appId;
        await WriteStateAsync();
        return true;
    }
}