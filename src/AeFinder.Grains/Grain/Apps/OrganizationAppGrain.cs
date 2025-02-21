using AeFinder.Grains.State.Apps;
using AeFinder.User.Eto;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Grains.Grain.Apps;

public class OrganizationAppGrain : AeFinderGrain<OrganizationAppState>, IOrganizationAppGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly AppSettingOptions _appSettingOptions;

    public OrganizationAppGrain(IDistributedEventBus distributedEventBus, IOptionsSnapshot<AppSettingOptions> appSettingOptions)
    {
        _distributedEventBus = distributedEventBus;
        _appSettingOptions = appSettingOptions.Value;
    }

    public async Task AddOrganizationAsync(string organizationName)
    {
        await ReadStateAsync();
        State.OrganizationId = this.GetPrimaryKeyString();
        State.OrganizationName = organizationName;
        await WriteStateAsync();
        
        //Publish organization create eto to background worker
        await _distributedEventBus.PublishAsync(new OrganizationCreateEto()
        {
            OrganizationId = State.OrganizationId,
            OrganizationName = State.OrganizationName,
            MaxAppCount = await GetMaxAppCountAsync()
        });
    }

    public async Task AddAppAsync(string appId)
    {
        await ReadStateAsync();
        var maxAppCount = State.MaxAppCount == 0 ? _appSettingOptions.MaxOrganizationAppCount : State.MaxAppCount;
        if (State.AppIds.Count >= maxAppCount)
        {
            throw new UserFriendlyException("The number of apps has reached the upper limit.");
        }

        State.AppIds.Add(appId);

        await WriteStateAsync();
    }

    public async Task DeleteAppAsync(string appId)
    {
        await ReadStateAsync();
        State.AppIds.Remove(appId);
        await WriteStateAsync();
    }

    public async Task<HashSet<string>> GetAppsAsync()
    {
        await ReadStateAsync();
        return State.AppIds;
    }

    public async Task<int> GetMaxAppCountAsync()
    {
        await ReadStateAsync();
        return State.MaxAppCount == 0 ? _appSettingOptions.MaxOrganizationAppCount : State.MaxAppCount;
    }

    public async Task SetMaxAppCountAsync(int maxAppCount)
    {
        await ReadStateAsync();
        State.OrganizationId = this.GetPrimaryKeyString();
        State.MaxAppCount = maxAppCount;
        await WriteStateAsync();
        
        //Publish organization max app count update eto to background worker
        await _distributedEventBus.PublishAsync(new MaxAppCountUpdateEto()
        {
            OrganizationId = State.OrganizationId,
            MaxAppCount = State.MaxAppCount
        });
    }
    
    public async Task<bool> CheckAppIsExistAsync(string appId)
    {
        return State.AppIds.Contains(appId);
    }
}