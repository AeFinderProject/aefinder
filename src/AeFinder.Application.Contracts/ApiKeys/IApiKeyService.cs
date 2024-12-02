using System;
using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiKeyService
{
    Task IncreaseQueryAeIndexerCountAsync(string key, string appId, string domain);
    Task IncreaseQueryBasicApiCountAsync(string key, BasicApi api, string domain);
    Task UpdateApiKeyInfoCacheAsync(ApiKeyInfo apiKeyInfo);
    Task UpdateApiKeyLimitCacheAsync(Guid organizationId, long limit);
    Task UpdateApiKeyUsedCacheAsync(Guid organizationId, long used);
    Task AddOrUpdateApiKeyIndexAsync(ApiKeyChangedEto input);
    Task AddOrUpdateApiKeyQueryAeIndexerIndexAsync(ApiKeyQueryAeIndexerChangedEto input);
    Task AddOrUpdateApiKeyQueryBasicApiIndexAsync(ApiKeyQueryBasicApiChangedEto input);
    Task AddOrUpdateApiKeySummaryIndexAsync(ApiKeySummaryChangedEto input);
}