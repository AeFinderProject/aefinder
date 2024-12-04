using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

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
    Task<ApiKeySummaryDto> GetApiKeySummaryAsync(Guid organizationId);
    Task<ApiKeyDto> CreateApiKeyAsync(Guid organizationId, CreateApiKeyInput input);
    Task<ApiKeyDto> GetApiKeyAsync(Guid organizationId, Guid apiKeyId);
    Task<PagedResultDto<ApiKeyDto>> GetApiKeysAsync(Guid organizationId, GetApiKeyInput input);
    Task RenameApiKeyAsync(Guid organizationId, Guid apiKeyId, string newName);
    Task<string> RegenerateKeyAsync(Guid organizationId, Guid apiKeyId);
    Task DeleteApiKeyAsync(Guid organizationId, Guid apiKeyId);
    Task SetSpendingLimitAsync(Guid organizationId, Guid apiKeyId, SetSpendingLimitInput input);
    Task SetAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input);
    Task DeleteAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input);
    Task SetAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input);
    Task DeleteAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input);
    Task SetAuthorisedApisAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedApiInput input);
    Task<PagedResultDto<ApiKeyQueryAeIndexerDto>> GetApiKeyQueryAeIndexersAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryAeIndexerInput input);
    Task<PagedResultDto<ApiKeyQueryApiDto>> GetApiKeyQueryApisAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryApiInput input);
}