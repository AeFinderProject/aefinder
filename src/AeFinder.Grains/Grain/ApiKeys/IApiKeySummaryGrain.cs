using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummaryGrain : IGrainWithStringKey
{
    Task IncreaseQueryLimitAsync(Guid organizationId, long query);
    Task RecordQueryAeIndexerCountAsync(Guid appKeyId, string appId, long query, DateTime dateTime);
    Task RecordQueryBasicDataCountAsync(Guid appKeyId, BasicDataApi basicDataApi, long query, DateTime dateTime);
}