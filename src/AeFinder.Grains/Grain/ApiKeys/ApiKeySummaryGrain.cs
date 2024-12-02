using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySummaryGrain : AeFinderGrain<ApiKeySummaryState>, IApiKeySummaryGrain
{
    private readonly IObjectMapper _objectMapper;

    public ApiKeySummaryGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
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