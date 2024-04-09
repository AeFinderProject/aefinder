using AeFinder.Grains.State.Apps;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppNameGrain : IGrainWithStringKey
{
    Task<string> Register(string adminId);
}

public class AppNameGrain : Grain<AppNameState>, IAppNameGrain
{
    public async Task<string> Register(string adminId)
    {
        if (adminId.IsNullOrEmpty() || State.AdminId != null)
        {
            return string.Empty;
        }

        State.AdminId = adminId;
        await WriteStateAsync();
        return adminId;
    }
}