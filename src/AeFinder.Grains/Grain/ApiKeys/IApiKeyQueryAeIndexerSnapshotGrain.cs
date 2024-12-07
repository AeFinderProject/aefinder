using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryAeIndexerSnapshotGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime, SnapshotType type);
    Task<long> GetQueryCountAsync();
}