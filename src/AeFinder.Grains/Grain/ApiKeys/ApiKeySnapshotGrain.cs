using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySnapshotGrain : AeFinderGrain<ApiKeySnapshotState>, IApiKeySnapshotGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid appKeyId, long query, DateTime dateTime,
        SnapshotType type)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = appKeyId;
        State.Query += query;
        State.Time = dateTime;
        State.Type = type;

        await WriteStateAsync();
    }
}