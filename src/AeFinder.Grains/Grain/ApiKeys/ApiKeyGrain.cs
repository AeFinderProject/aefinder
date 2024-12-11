using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyGrain : AeFinderGrain<ApiKeyState>, IApiKeyGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClock _clock;
    private readonly IDistributedEventBus _distributedEventBus;

    public ApiKeyGrain(IObjectMapper objectMapper, IClock clock, IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _clock = clock;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<ApiKeyInfo> CreateAsync(Guid id, Guid organizationId, CreateApiKeyInput input)
    {
        await ReadStateAsync();
        State.Id = id;
        State.OrganizationId = organizationId;
        State.Name = input.Name;
        State.Key = GenerateKey();
        State.IsEnableSpendingLimit = input.IsEnableSpendingLimit;
        State.SpendingLimitUsdt = input.SpendingLimitUsdt;
        State.CreateTime = _clock.Now;
        await WriteStateAsync();

        return _objectMapper.Map<ApiKeyState, ApiKeyInfo>(State);
    }

    public async Task<ApiKeyInfo> UpdateAsync(UpdateApiKeyInput input)
    {
        await ReadStateAsync();
        if (!input.Name.IsNullOrWhiteSpace())
        {
            State.Name = input.Name;
        }

        if (input.IsEnableSpendingLimit.HasValue)
        {
            State.IsEnableSpendingLimit = input.IsEnableSpendingLimit.Value;
        }

        if (input.SpendingLimitUsdt.HasValue)
        {
            State.SpendingLimitUsdt = input.SpendingLimitUsdt.Value;
        }

        await WriteStateAsync();

        return _objectMapper.Map<ApiKeyState, ApiKeyInfo>(State);
    }

    public async Task<string> RegenerateKeyAsync()
    {
        await ReadStateAsync();
        State.Key = GenerateKey();
        await WriteStateAsync();

        return State.Key;
    }

    public async Task DeleteAsync()
    {
        await ReadStateAsync();
        State.IsDeleted = true;
        await WriteStateAsync();
    }

    public async Task SetAuthorisedAeIndexersAsync(List<string> appIds)
    {
        await ReadStateAsync();
        foreach (var appId in appIds)
        {
            var appGrain = GrainFactory.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
            var appName = (await appGrain.GetAsync()).AppName;
            State.AuthorisedAeIndexers[appId] = new AppInfoImmutable
            {
                AppId = appId,
                AppName = appName
            };
        }
        await WriteStateAsync();
    }

    public async Task DeleteAuthorisedAeIndexersAsync(List<string> appIds)
    {
        await ReadStateAsync();
        foreach (var appId in appIds)
        {
            State.AuthorisedAeIndexers.Remove(appId);
        }
        await WriteStateAsync();
    }
    
    public async Task SetAuthorisedApisAsync(Dictionary<BasicApi,bool> apis)
    {
        await ReadStateAsync();
        foreach (var api in apis)
        {
            if (api.Value)
            {
                State.AuthorisedApis.Add(api.Key);
            }
            else
            {
                State.AuthorisedApis.Remove(api.Key);
            }
        }
        await WriteStateAsync();
    }

    public async Task SetAuthorisedDomainsAsync(List<string> domains)
    {
        await ReadStateAsync();
        foreach (var domain in domains)
        {
            State.AuthorisedDomains.Add(domain);
        }
        await WriteStateAsync();
    }

    public async Task DeleteAuthorisedDomainsAsync(List<string> domains)
    {
        await ReadStateAsync();
        foreach (var domain in domains)
        {
            State.AuthorisedDomains.Remove(domain);
        }
        await WriteStateAsync();
    }

    public async Task RecordQueryCountAsync(long query, DateTime dateTime)
    {
        await ReadStateAsync();

        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.ToMonthDate();
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(State.Id, monthlyDate);
        await GrainFactory.GetGrain<IApiKeySnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(State.OrganizationId, State.Id, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyDailySnapshotGrainId(State.Id, dailyDate);
        await GrainFactory.GetGrain<IApiKeySnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(State.OrganizationId, State.Id, query, dailyDate, SnapshotType.Daily);
    }

    public async Task<ApiKeyInfo> GetAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<ApiKeyState, ApiKeyInfo>(State);
    }

    public async Task<long?> GetAvailabilityQueryAsync(DateTime dateTime)
    {
        await ReadStateAsync();

        if (!State.IsEnableSpendingLimit)
        {
            return null;
        }
        
        return await CalculateAvailabilityQueryAsync(dateTime);
    }

    protected override async Task WriteStateAsync()
    {
        State.UpdateTime = _clock.Now;
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<ApiKeyState, ApiKeyChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }

    private async Task<long> CalculateAvailabilityQueryAsync(DateTime dateTime)
    {
        var monthlyDate = dateTime.ToMonthDate();
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(State.Id, monthlyDate);
        var periodQuery = await GrainFactory.GetGrain<IApiKeySnapshotGrain>(monthlySnapshotKey).GetQueryCountAsync();

        return (long)(State.SpendingLimitUsdt / AeFinderApplicationConsts.ApiKeyQueryPrice) - periodQuery;
    }

    private string GenerateKey()
    {
        return Guid.NewGuid().ToString("N");
    }
}