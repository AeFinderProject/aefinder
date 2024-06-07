using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IOrganizationAppGrain : IGrainWithStringKey
{
    Task AddAppAsync(string appId);
    Task<HashSet<string>> GetAppsAsync();
}