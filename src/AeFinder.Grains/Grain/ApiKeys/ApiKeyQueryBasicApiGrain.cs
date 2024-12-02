using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryBasicApiGrain : AeFinderGrain<ApiKeyQueryBasicApiState>, IApiKeyQueryBasicApiGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicApi api, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.Api = api;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicApiMonthlySnapshotGrainId(apiKeyId, api, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, api, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicApiDailySnapshotGrainId(apiKeyId, api, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicApiSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, api, query, dailyDate, SnapshotType.Daily);
    }
}