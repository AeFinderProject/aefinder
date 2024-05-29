using AeFinder.Grains.State.Apps;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public class OrganizationAppGrain : Grain<OrganizationAppState>, IOrganizationAppGrain
{
    public async Task AddAppAsync(string appId)
    {
        State.AppIds.Add(appId);
        await WriteStateAsync();
    }

    public Task<HashSet<string>> GetAppsAsync()
    {
        return Task.FromResult(State.AppIds);
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
}