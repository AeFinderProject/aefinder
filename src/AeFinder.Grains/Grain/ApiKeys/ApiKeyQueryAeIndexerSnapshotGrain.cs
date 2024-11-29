using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotGrain : AeFinderGrain<ApiKeyQueryAeIndexerSnapshotState>,
    IApiKeyQueryAeIndexerSnapshotGrain
{
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

        await WriteStateAsync();
    }
}