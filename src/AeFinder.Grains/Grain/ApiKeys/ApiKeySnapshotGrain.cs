using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySnapshotGrain : AeFinderGrain<ApiKeySnapshotState>, IApiKeySnapshotGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ApiKeySnapshotGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, long query, DateTime dateTime,
        SnapshotType type)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.Query += query;
        State.Time = dateTime;
        State.Type = type;

        await WriteStateAsync();
    }

    public async Task<long> GetQueryCountAsync()
    {
        await ReadStateAsync();
        return State.Query;
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData =
            _objectMapper.Map<ApiKeySnapshotState, ApiKeySnapshotChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}