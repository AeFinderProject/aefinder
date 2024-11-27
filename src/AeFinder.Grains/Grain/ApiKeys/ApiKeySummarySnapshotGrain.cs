using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeySummarySnapshotGrain : AeFinderGrain<ApiKeySummarySnapshotState>, IApiKeySummarySnapshotGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, long query, DateTime dateTime, SnapshotType type)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
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