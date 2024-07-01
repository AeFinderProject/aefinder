using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IOrganizationAppGrain : IGrainWithStringKey
{
    Task AddAppAsync(string appId);
    Task<HashSet<string>> GetAppsAsync();
    Task<int> GetMaxAppCountAsync();
    Task SetMaxAppCountAsync(int maxAppCount);
}