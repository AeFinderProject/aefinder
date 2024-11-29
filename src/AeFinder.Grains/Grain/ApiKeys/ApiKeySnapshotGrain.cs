using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySnapshotGrain : AeFinderGrain<ApiKeySnapshotState>, IApiKeySnapshotGrain
{
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
}