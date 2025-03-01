using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IOrganizationAppGrain : IGrainWithStringKey
{
    Task AddOrganizationAsync(string organizationName);
    Task AddAppAsync(string appId);
    Task DeleteAppAsync(string appId);
    Task<HashSet<string>> GetAppsAsync();
    Task<int> GetMaxAppCountAsync();
    Task SetMaxAppCountAsync(int maxAppCount);
    Task<bool> CheckAppIsExistAsync(string appId);
}