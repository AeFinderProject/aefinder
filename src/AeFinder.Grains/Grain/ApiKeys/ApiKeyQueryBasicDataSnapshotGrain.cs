using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryBasicDataSnapshotGrain : AeFinderGrain<ApiKeyQueryBasicDataSnapshotState>,
    IApiKeyQueryBasicDataSnapshotGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicDataApiType basicDataApiType, long query,
        DateTime dateTime, SnapshotType type)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.BasicDataApiType = basicDataApiType;
        State.Query += query;
        State.Time = dateTime;
        State.Type = type;

        await WriteStateAsync();
    }
}