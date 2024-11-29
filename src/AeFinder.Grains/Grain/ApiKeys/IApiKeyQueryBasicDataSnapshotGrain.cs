using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryBasicDataSnapshotGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicDataApiType basicDataApiType, long query,
        DateTime dateTime, SnapshotType type);
}