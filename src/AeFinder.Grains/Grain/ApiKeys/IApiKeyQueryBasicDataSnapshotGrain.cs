using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryBasicDataSnapshotGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid appKeyId, BasicDataApi basicDataApi, long query,
        DateTime dateTime, SnapshotType type);
}