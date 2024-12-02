using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummaryGrain : IGrainWithStringKey
{
    Task IncreaseQueryLimitAsync(Guid organizationId, long query);
    Task RecordQueryAeIndexerCountAsync(Guid apiKeyId, string appId, long query, DateTime dateTime);
    Task RecordQueryBasicApiCountAsync(Guid apiKeyId, BasicApi api, long query, DateTime dateTime);
    Task<ApiKeySummaryInfo> GetApiKeySummaryInfoAsync();
    Task<ApiKeyInfo> CreateApiKeyAsync(Guid apiKeyId, Guid organizationId, string name);
    Task DeleteApiKeyAsync(Guid apiKeyId);
}