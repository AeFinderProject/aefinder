using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace AeFinder.ApiKeys;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiKeyService : AeFinderAppService, IApiKeyService
{
    private readonly IApiKeyTrafficProvider _apiKeyTrafficProvider;
    private readonly IApiKeyInfoProvider _apiKeyInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<ApiKeyIndex, Guid> _apiKeyIndexRepository;
    private readonly IEntityMappingRepository<ApiKeyQueryAeIndexerIndex, string> _apiKeyQueryAeIndexerIndexRepository;
    private readonly IEntityMappingRepository<ApiKeyQueryBasicApiIndex, string> _apiKeyQueryBasicApiIndexRepository;
    private readonly IEntityMappingRepository<ApiKeySummaryIndex, string> _apiKeySummaryIndexRepository;

    public ApiKeyService(IApiKeyTrafficProvider apiKeyTrafficProvider, IClusterClient clusterClient,
        IApiKeyInfoProvider apiKeyInfoProvider, IEntityMappingRepository<ApiKeyIndex, Guid> apiKeyIndexRepository,
        IEntityMappingRepository<ApiKeyQueryAeIndexerIndex, string> apiKeyQueryAeIndexerIndexRepository,
        IEntityMappingRepository<ApiKeyQueryBasicApiIndex, string> apiKeyQueryBasicApiIndexRepository,
        IEntityMappingRepository<ApiKeySummaryIndex, string> apiKeySummaryIndexRepository)
    {
        _apiKeyTrafficProvider = apiKeyTrafficProvider;
        _clusterClient = clusterClient;
        _apiKeyInfoProvider = apiKeyInfoProvider;
        _apiKeyIndexRepository = apiKeyIndexRepository;
        _apiKeyQueryAeIndexerIndexRepository = apiKeyQueryAeIndexerIndexRepository;
        _apiKeyQueryBasicApiIndexRepository = apiKeyQueryBasicApiIndexRepository;
        _apiKeySummaryIndexRepository = apiKeySummaryIndexRepository;
    }

    public async Task IncreaseQueryAeIndexerCountAsync(string key, string appId, string domain)
    {
        await _apiKeyTrafficProvider.IncreaseAeIndexerQueryAsync(key, appId, domain);
    }

    public async Task IncreaseQueryBasicApiCountAsync(string key, BasicApi api, string domain)
    {
        await _apiKeyTrafficProvider.IncreaseBasicApiQueryAsync(key, api, domain);
    }

    public async Task UpdateApiKeyInfoCacheAsync(ApiKeyInfo apiKeyInfo)
    {
        await _apiKeyInfoProvider.SetApiKeyInfoAsync(apiKeyInfo);
    }

    public async Task UpdateApiKeyLimitCacheAsync(Guid organizationId, long limit)
    {
        await _apiKeyInfoProvider.SetApiKeyLimitAsync(organizationId, limit);
    }

    public async Task UpdateApiKeyUsedCacheAsync(Guid organizationId, long used)
    {
        await _apiKeyInfoProvider.SetApiKeyUsedAsync(organizationId, used);
    }

    public async Task AddOrUpdateApiKeyIndexAsync(ApiKeyChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyChangedEto, ApiKeyIndex>(input);
        await _apiKeyIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeyQueryAeIndexerIndexAsync(ApiKeyQueryAeIndexerChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryAeIndexerChangedEto, ApiKeyQueryAeIndexerIndex>(input);
        await _apiKeyQueryAeIndexerIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeyQueryBasicApiIndexAsync(ApiKeyQueryBasicApiChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryBasicApiChangedEto, ApiKeyQueryBasicApiIndex>(input);
        await _apiKeyQueryBasicApiIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeySummaryIndexAsync(ApiKeySummaryChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeySummaryChangedEto, ApiKeySummaryIndex>(input);
        await _apiKeySummaryIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<ApiKeySummaryDto> GetApiKeySummaryAsync(Guid organizationId)
    {
        var summary = await _apiKeySummaryIndexRepository.GetAsync(GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
        return ObjectMapper.Map<ApiKeySummaryIndex, ApiKeySummaryDto>(summary);
    }

    public Task<ApiKeyDto> CreateApiKeyAsync(Guid organizationId, CreateApiKeyInput input)
    {
        throw new NotImplementedException();
    }

    public Task<ApiKeyDto> GetApiKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<ApiKeyDto>> GetApiKeysAsync(Guid organizationId, GetApiKeyInput input)
    {
        throw new NotImplementedException();
    }

    public Task RenameApiKeyAsync(Guid organizationId, Guid apiKeyId, string newName)
    {
        throw new NotImplementedException();
    }

    public Task<string> RegenerateKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteApiKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        throw new NotImplementedException();
    }

    public Task SetSpendingLimitAsync(Guid organizationId, Guid apiKeyId, SetSpendingLimitInput input)
    {
        throw new NotImplementedException();
    }

    public Task SetAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input)
    {
        throw new NotImplementedException();
    }

    public Task SetAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input)
    {
        throw new NotImplementedException();
    }

    public Task SetAuthorisedApisAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedApiInput input)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<ApiKeyQueryAeIndexerDto>> GetApiKeyQueryAeIndexersAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryAeIndexerInput input)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResultDto<ApiKeyQueryApiDto>> GetApiKeyQueryApisAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryApiInput input)
    {
        throw new NotImplementedException();
    }
}