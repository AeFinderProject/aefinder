using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyGrain : AeFinderGrain<ApiKeyState>, IApiKeyGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClock _clock;
    
    // TODO: Get prices through product information
    private const decimal QueryPrice = 4 / 100000;

    public ApiKeyGrain(IObjectMapper objectMapper, IClock clock)
    {
        _objectMapper = objectMapper;
        _clock = clock;
    }

    public async Task<ApiKeyInfo> CreateAsync(Guid id, Guid organizationId, string name)
    {
        await ReadStateAsync();
        State.Id = id;
        State.OrganizationId = organizationId;
        State.Name = name;
        State.Key = GenerateKey();
        State.Status = ApiKeyStatus.Active;
        await WriteStateAsync();

        return _objectMapper.Map<ApiKeyState, ApiKeyInfo>(State);
    }

    public async Task RenameAsync(string newName)
    {
        await ReadStateAsync();
        State.Name = newName;
        await WriteStateAsync();
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

    public async Task SetSpendingLimitAsync(bool isEnable, decimal limitUsdt)
    {
        await ReadStateAsync();
        State.IsEnableSpendingLimit = isEnable;
        State.SpendingLimitUsdt = limitUsdt;
        
        var availabilityQuery = await CalculateAvailabilityQueryAsync(_clock.Now);
        State.Status = availabilityQuery > 0 ? ApiKeyStatus.Active : ApiKeyStatus.Stopped;
        
        await WriteStateAsync();
    }

    public async Task SetAuthorisedAeIndexersAsync(List<string> appIds)
    {
        await ReadStateAsync();
        foreach (var appId in appIds)
        {
            State.AuthorisedAeIndexers.Add(appId);
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
        
        var availabilityQuery = await GetAvailabilityQueryAsync(dateTime);
        if (availabilityQuery.HasValue && query >= availabilityQuery)
        {
            State.Status = ApiKeyStatus.Stopped;
        }

        await WriteStateAsync();
        
        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
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

    private async Task<long> CalculateAvailabilityQueryAsync(DateTime dateTime)
    {
        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(State.Id, monthlyDate);
        var periodQuery = await GrainFactory.GetGrain<IApiKeySnapshotGrain>(monthlySnapshotKey).GetQueryCountAsync();

        return (long)(State.SpendingLimitUsdt / QueryPrice) - periodQuery;
    }

    private string GenerateKey()
    {
        return Guid.NewGuid().ToString("N");
    }
}