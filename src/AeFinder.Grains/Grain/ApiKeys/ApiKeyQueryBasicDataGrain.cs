using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryBasicDataGrain : AeFinderGrain<ApiKeyQueryBasicDataState>, IApiKeyQueryBasicDataGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid appKeyId, BasicDataApi basicDataApi, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = appKeyId;
        State.BasicDataApi = basicDataApi;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicDataMonthlySnapshotGrainId(appKeyId, basicDataApi, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicDataSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, appKeyId, basicDataApi, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicDataDailySnapshotGrainId(appKeyId, basicDataApi, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicDataSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, appKeyId, basicDataApi, query, dailyDate, SnapshotType.Daily);
    }
}