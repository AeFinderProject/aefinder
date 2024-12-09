using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryBasicApiGrain : AeFinderGrain<ApiKeyQueryBasicApiState>, IApiKeyQueryBasicApiGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ApiKeyQueryBasicApiGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }
    
    public async Task<ApiKeyQueryBasicApiInfo> GetAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<ApiKeyQueryBasicApiState, ApiKeyQueryBasicApiInfo>(State);
    }

    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicApi api, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.Api = api;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.ToMonthDate();
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(apiKeyId, api, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, api, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicApiDailySnapshotGrainId(apiKeyId, api, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, api, query, dailyDate, SnapshotType.Daily);
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData =
            _objectMapper.Map<ApiKeyQueryBasicApiState, ApiKeyQueryBasicApiChangedEto>(State);
        eventData.Id = this.GetPrimaryKeyString();
        await _distributedEventBus.PublishAsync(eventData);
    }
}