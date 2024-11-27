using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummarySnapshotGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, long query, DateTime dateTime, SnapshotType type);
    Task<long> GetQueryCountAsync();
}