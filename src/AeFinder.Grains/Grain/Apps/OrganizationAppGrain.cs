using AeFinder.Grains.State.Apps;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;

namespace AeFinder.Grains.Grain.Apps;

public class OrganizationAppGrain : Grain<OrganizationAppState>, IOrganizationAppGrain
{
    private readonly AppSettingOptions _appSettingOptions;

    public OrganizationAppGrain(IOptionsSnapshot<AppSettingOptions> appSettingOptions)
    {
        _appSettingOptions = appSettingOptions.Value;
    }

    public async Task AddAppAsync(string appId)
    {
        var maxAppCount = State.MaxAppCount == 0 ? _appSettingOptions.MaxOrganizationAppCount : State.MaxAppCount;
        if (State.AppIds.Count >= maxAppCount)
        {
            throw new UserFriendlyException("The number of apps has reached the upper limit.");
        }

        State.AppIds.Add(appId);
        await WriteStateAsync();
    }

    public async Task<HashSet<string>> GetAppsAsync()
    {
        await ReadStateAsync();
        return State.AppIds;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<int> GetMaxAppCountAsync()
    {
        await ReadStateAsync();
        return State.MaxAppCount == 0 ? _appSettingOptions.MaxOrganizationAppCount : State.MaxAppCount;
    }

    public async Task SetMaxAppCountAsync(int maxAppCount)
    {
        State.MaxAppCount = maxAppCount;
        await WriteStateAsync();
    }
}