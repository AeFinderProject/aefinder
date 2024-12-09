using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySnapshotGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, long query, DateTime dateTime, SnapshotType type);
    Task<long> GetQueryCountAsync();
}