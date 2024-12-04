using AeFinder.ApiKeys;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerGrain : AeFinderGrain<ApiKeyQueryAeIndexerState>, IApiKeyQueryAeIndexerGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ApiKeyQueryAeIndexerGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.AppId = appId;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;
        
        var appGrain = GrainFactory.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        State.AppName = (await appGrain.GetAsync()).AppName;

        await WriteStateAsync();

        var monthlyDate = dateTime.ToMonthDate();
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(apiKeyId, appId, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, appId, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryAeIndexerDailySnapshotGrainId(apiKeyId, appId, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, appId, query, dailyDate, SnapshotType.Daily);
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<ApiKeyQueryAeIndexerState, ApiKeyQueryAeIndexerChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}