using System;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
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
        IApiKeyInfoProvider apiKeyInfoProvider)
    {
        _apiKeyTrafficProvider = apiKeyTrafficProvider;
        _clusterClient = clusterClient;
        _apiKeyInfoProvider = apiKeyInfoProvider;
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

    public Task AddOrUpdateApiKeyIndexAsync(ApiKeyEto input)
    {
        throw new NotImplementedException();
    }

    public Task AddOrUpdateApiKeyQueryAeIndexerIndexAsync(ApiKeyQueryAeIndexerEto input)
    {
        throw new NotImplementedException();
    }

    public Task AddOrUpdateApiKeyQueryBasicApiIndexAsync(ApiKeyQueryBasicApiEto input)
    {
        throw new NotImplementedException();
    }

    public Task AddOrUpdateApiKeySummaryIndexAsync(ApiKeySummaryEto input)
    {
        throw new NotImplementedException();
    }
}