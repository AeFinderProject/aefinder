using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.ApiKeys;

public interface IApiKeyService
{
    Task IncreaseQueryAeIndexerCountAsync(string key, string appId, string domain, DateTime dateTime);
    Task IncreaseQueryBasicApiCountAsync(string key, BasicApi api, string domain, DateTime dateTime);
    Task UpdateApiKeyInfoCacheAsync(ApiKeyInfo apiKeyInfo);
    Task UpdateApiKeySummaryLimitCacheAsync(Guid organizationId, long limit);
    Task UpdateApiKeySummaryUsedCacheAsync(Guid organizationId, DateTime dateTime, long used);
    Task UpdateApiKeyUsedCacheAsync(Guid apiKeyId, DateTime dateTime, long used);
    Task AddOrUpdateApiKeySummaryIndexAsync(ApiKeySummaryChangedEto input);
    Task AddOrUpdateApiKeyIndexAsync(ApiKeyChangedEto input);
    Task AddOrUpdateApiKeyQueryAeIndexerIndexAsync(ApiKeyQueryAeIndexerChangedEto input);
    Task AddOrUpdateApiKeyQueryBasicApiIndexAsync(ApiKeyQueryBasicApiChangedEto input);
    Task<ApiKeySummaryDto> GetApiKeySummaryAsync(Guid organizationId);
    Task<ApiKeyDto> CreateApiKeyAsync(Guid organizationId, CreateApiKeyInput input);
    Task<ApiKeyDto> UpdateApiKeyAsync(Guid organizationId, Guid apiKeyId, UpdateApiKeyInput input);
    Task<ApiKeyDto> GetApiKeyAsync(Guid organizationId, Guid apiKeyId);
    Task<PagedResultDto<ApiKeyDto>> GetApiKeysAsync(Guid organizationId, GetApiKeyInput input);
    Task<RegenerateKeyDto> RegenerateKeyAsync(Guid organizationId, Guid apiKeyId);
    Task DeleteApiKeyAsync(Guid organizationId, Guid apiKeyId);
    Task SetAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input);
    Task DeleteAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input);
    Task SetAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input);
    Task DeleteAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input);
    Task SetAuthorisedApisAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedApiInput input);
    Task<PagedResultDto<ApiKeyQueryAeIndexerDto>> GetApiKeyQueryAeIndexersAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryAeIndexerInput input);
    Task<PagedResultDto<ApiKeyQueryApiDto>> GetApiKeyQueryApisAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryApiInput input);
    Task AdjustQueryLimitAsync(Guid organizationId, long count);
    Task<long> GetMonthQueryCountAsync(Guid orgId, DateTime time);

}