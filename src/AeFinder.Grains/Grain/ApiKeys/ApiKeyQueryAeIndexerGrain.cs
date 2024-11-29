using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerGrain : AeFinderGrain<ApiKeyQueryAeIndexerState>, IApiKeyQueryAeIndexerGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.AppId = appId;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryAeIndexerMonthlySnapshotGrainId(apiKeyId, appId, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, appId, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryAeIndexerDailySnapshotGrainId(apiKeyId, appId, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryAeIndexerSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, appId, query, dailyDate, SnapshotType.Daily);
    }
}