using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummaryGrain : IGrainWithStringKey
{
    Task IncreaseQueryLimitAsync(Guid organizationId, long query);
    Task RecordQueryAeIndexerCountAsync(Guid apiKeyId, string appId, long query, DateTime dateTime);
    Task RecordQueryBasicDataCountAsync(Guid apiKeyId, BasicDataApiType basicDataApiType, long query, DateTime dateTime);
    Task<ApiKeySummaryInfo> GetApiKeySummaryInfoAsync();

}