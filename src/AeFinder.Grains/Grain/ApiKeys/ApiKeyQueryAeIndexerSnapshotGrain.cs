using AeFinder.ApiKeys;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotGrain : AeFinderGrain<ApiKeyQueryAeIndexerSnapshotState>,
    IApiKeyQueryAeIndexerSnapshotGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ApiKeyQueryAeIndexerSnapshotGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime, SnapshotType type)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.AppId = appId;
        State.Query += query;
        State.Time = dateTime;
        State.Type = type;

        var appGrain = GrainFactory.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        State.AppName = (await appGrain.GetAsync()).AppName;

        await WriteStateAsync();
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData =
            _objectMapper.Map<ApiKeyQueryAeIndexerSnapshotState, ApiKeyQueryAeIndexerSnapshotChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}