using System;
using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiKeyService
{
    Task IncreaseQueryAeIndexerCountAsync(string key, string appId, string domain);
    Task IncreaseQueryBasicDataCountAsync(string key, BasicDataApiType basicDataApiType, string domain);
    Task UpdateApiKeyInfoCacheAsync(ApiKeyInfo apiKeyInfo);
    Task UpdateApiKeyLimitCacheAsync(Guid organizationId, long limit);
    Task UpdateApiKeyUsedCacheAsync(Guid organizationId, long used);
}