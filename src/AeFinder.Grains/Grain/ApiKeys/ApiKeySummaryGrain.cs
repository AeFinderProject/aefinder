using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySummaryGrain : AeFinderGrain<ApiKeySummaryState>, IApiKeySummaryGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ApiKeyOptions _apiKeyOptions;

    public ApiKeySummaryGrain(IObjectMapper objectMapper, IOptionsSnapshot<ApiKeyOptions> apiKeyOptions)
    {
        _objectMapper = objectMapper;
        _apiKeyOptions = apiKeyOptions.Value;
    }

    public async Task IncreaseQueryLimitAsync(Guid organizationId, long query)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.QueryLimit += query;

        await WriteStateAsync();
    }
    
    public async Task RecordQueryAeIndexerCountAsync(Guid apiKeyId, string appId, long query, DateTime dateTime)
    {
        var apiKeyGrain = GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await RecordQueryCountAsync(apiKeyInfo, query, dateTime);

        await GrainFactory
            .GetGrain<IApiKeyQueryAeIndexerGrain>(
                GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKeyId, appId))
            .RecordQueryCountAsync(apiKeyInfo.OrganizationId, apiKeyId, appId, query, dateTime);
    }
    
    public async Task RecordQueryBasicApiCountAsync(Guid apiKeyId, BasicApi api, long query, DateTime dateTime)
    {
        var apiKeyGrain = GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        await RecordQueryCountAsync(apiKeyInfo, query, dateTime);

        await GrainFactory
            .GetGrain<IApiKeyQueryBasicApiGrain>(
                GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKeyId, api))
            .RecordQueryCountAsync(apiKeyInfo.OrganizationId, apiKeyId, api, query, dateTime);
    }

    public async Task<ApiKeySummaryInfo> GetApiKeySummaryInfoAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<ApiKeySummaryState, ApiKeySummaryInfo>(State);
    }

    public async Task<ApiKeyInfo> CreateApiKeyAsync(Guid apiKeyId, Guid organizationId, string name)
    {
        await ReadStateAsync();
        if (State.ApiKeyCount + 1 > _apiKeyOptions.MaxApiKeyCount)
        {
            throw new UserFriendlyException($"Api Key reached the upper limit!");
        }

        State.ApiKeyCount += 1;

        var apiKeyInfo = await GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId).CreateAsync(apiKeyId, organizationId, name);

        await WriteStateAsync();
        return apiKeyInfo;
    }

    public async Task DeleteApiKeyAsync(Guid apiKeyId)
    {
        await ReadStateAsync();
        
        State.ApiKeyCount -= 1;
        await GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId).DeleteAsync();

        await WriteStateAsync();
    }

    private async Task<bool> RecordQueryCountAsync(ApiKeyInfo apiKeyInfo, long query, DateTime dateTime)
    {
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(apiKeyInfo.OrganizationId, dateTime);
        var monthlySnapshotGrain = GrainFactory.GetGrain<IApiKeySummarySnapshotGrain>(monthlySnapshotKey);
        var periodQuery = await monthlySnapshotGrain.GetQueryCountAsync();

        await ReadStateAsync();

        var apiKeyGrain = GrainFactory.GetGrain<IApiKeyGrain>(apiKeyInfo.Id);
        var availabilityQuery = await apiKeyGrain.GetAvailabilityQueryAsync(dateTime);
        if (periodQuery >= State.QueryLimit || apiKeyInfo.Status == ApiKeyStatus.Stopped ||
            availabilityQuery is <= 0)
        {
            return false;
        }

        if (periodQuery + query > State.QueryLimit)
        {
            query = State.QueryLimit - periodQuery;
        }
        
        if (availabilityQuery.HasValue)
        {
            query = Math.Min(query, availabilityQuery.Value);
        }

        State.TotalQuery += query;
        State.LastQueryTime = dateTime;
        await WriteStateAsync();
        
        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        await monthlySnapshotGrain.RecordQueryCountAsync(apiKeyInfo.OrganizationId, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(apiKeyInfo.OrganizationId, dailyDate);
        await GrainFactory.GetGrain<IApiKeySummarySnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(apiKeyInfo.OrganizationId, query, dailyDate, SnapshotType.Daily);

        await apiKeyGrain.RecordQueryCountAsync(query, dateTime);

        return true;
    }
}