using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySummaryGrain : AeFinderGrain<ApiKeySummaryState>, IApiKeySummaryGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ApiKeyOptions _apiKeyOptions;
    private readonly IDistributedEventBus _distributedEventBus;

    public ApiKeySummaryGrain(IObjectMapper objectMapper, IOptionsSnapshot<ApiKeyOptions> apiKeyOptions,
        IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _apiKeyOptions = apiKeyOptions.Value;
    }

    public async Task AdjustQueryLimitAsync(Guid organizationId, long query)
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

        var recordQuery = await RecordQueryCountAsync(apiKeyInfo, query, dateTime);
        if (recordQuery > 0)
        {
            await GrainFactory
                .GetGrain<IApiKeyQueryAeIndexerGrain>(
                    GrainIdHelper.GenerateApiKeyQueryAeIndexerGrainId(apiKeyId, appId))
                .RecordQueryCountAsync(apiKeyInfo.OrganizationId, apiKeyId, appId, recordQuery, dateTime);
        }
    }
    
    public async Task RecordQueryBasicApiCountAsync(Guid apiKeyId, BasicApi api, long query, DateTime dateTime)
    {
        var apiKeyGrain = GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId);
        var apiKeyInfo = await apiKeyGrain.GetAsync();

        var recordQuery = await RecordQueryCountAsync(apiKeyInfo, query, dateTime);
        if (recordQuery > 0)
        {
            await GrainFactory
                .GetGrain<IApiKeyQueryBasicApiGrain>(
                    GrainIdHelper.GenerateApiKeyQueryBasicApiGrainId(apiKeyId, api))
                .RecordQueryCountAsync(apiKeyInfo.OrganizationId, apiKeyId, api, recordQuery, dateTime);
        }
    }

    public async Task<ApiKeySummaryInfo> GetApiKeySummaryInfoAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<ApiKeySummaryState, ApiKeySummaryInfo>(State);
    }

    public async Task<ApiKeyInfo> CreateApiKeyAsync(Guid apiKeyId, Guid organizationId, CreateApiKeyInput input)
    {
        await ReadStateAsync();
        if (State.ApiKeyCount + 1 > _apiKeyOptions.MaxApiKeyCount)
        {
            throw new UserFriendlyException($"Api Key reached the upper limit!");
        }

        State.OrganizationId = organizationId;
        State.ApiKeyCount += 1;

        var apiKeyInfo = await GrainFactory.GetGrain<IApiKeyGrain>(apiKeyId).CreateAsync(apiKeyId, organizationId, input);

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

    private async Task<long> RecordQueryCountAsync(ApiKeyInfo apiKeyInfo, long query, DateTime dateTime)
    {
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(apiKeyInfo.OrganizationId, dateTime);
        var monthlySnapshotGrain = GrainFactory.GetGrain<IApiKeySummarySnapshotGrain>(monthlySnapshotKey);
        var periodQuery = await monthlySnapshotGrain.GetQueryCountAsync();

        await ReadStateAsync();

        var apiKeyGrain = GrainFactory.GetGrain<IApiKeyGrain>(apiKeyInfo.Id);
        var availabilityQuery = await apiKeyGrain.GetAvailabilityQueryAsync(dateTime);
        if (periodQuery >= State.QueryLimit || availabilityQuery is <= 0)
        {
            return 0;
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

        var monthlyDate = dateTime.ToMonthDate();
        await monthlySnapshotGrain.RecordQueryCountAsync(apiKeyInfo.OrganizationId, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeySummaryDailySnapshotGrainId(apiKeyInfo.OrganizationId, dailyDate);
        await GrainFactory.GetGrain<IApiKeySummarySnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(apiKeyInfo.OrganizationId, query, dailyDate, SnapshotType.Daily);

        await apiKeyGrain.RecordQueryCountAsync(query, dateTime);

        return query;
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData =
            _objectMapper.Map<ApiKeySummaryState, ApiKeySummaryChangedEto>(State);
        eventData.Id = this.GetPrimaryKeyString();
        await _distributedEventBus.PublishAsync(eventData);
    }
}