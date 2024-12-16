using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummaryGrain : IGrainWithStringKey
{
    Task SetQueryLimitAsync(Guid organizationId, long query);
    Task RecordQueryAeIndexerCountAsync(Guid apiKeyId, string appId, long query, DateTime dateTime);
    Task RecordQueryBasicApiCountAsync(Guid apiKeyId, BasicApi api, long query, DateTime dateTime);
    Task<ApiKeySummaryInfo> GetApiKeySummaryInfoAsync();
    Task<ApiKeyInfo> CreateApiKeyAsync(Guid apiKeyId, Guid organizationId, CreateApiKeyInput input);
    Task DeleteApiKeyAsync(Guid apiKeyId);
}