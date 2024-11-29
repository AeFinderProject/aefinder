using AeFinder.ApiKeys;
using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryBasicDataGrain : AeFinderGrain<ApiKeyQueryBasicDataState>, IApiKeyQueryBasicDataGrain
{
    public async Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicDataApiType basicDataApiType, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = apiKeyId;
        State.BasicDataApiType = basicDataApiType;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        var monthlyDate = dateTime.Date.AddDays(-dateTime.Day + 1);
        var monthlySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicDataMonthlySnapshotGrainId(apiKeyId, basicDataApiType, monthlyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicDataSnapshotGrain>(monthlySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, basicDataApiType, query, monthlyDate, SnapshotType.Monthly);

        var dailyDate = dateTime.Date;
        var dailySnapshotKey =
            GrainIdHelper.GenerateApiKeyQueryBasicDataDailySnapshotGrainId(apiKeyId, basicDataApiType, dailyDate);
        await GrainFactory.GetGrain<IApiKeyQueryBasicDataSnapshotGrain>(dailySnapshotKey)
            .RecordQueryCountAsync(organizationId, apiKeyId, basicDataApiType, query, dailyDate, SnapshotType.Daily);
    }
}