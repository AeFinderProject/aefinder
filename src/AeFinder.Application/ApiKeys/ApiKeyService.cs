using System;
using System.Threading.Tasks;
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

    public async Task IncreaseQueryBasicDataCountAsync(string key, BasicDataApiType basicDataApiType, string domain)
    {
        await _apiKeyTrafficProvider.IncreaseBasicDataQueryAsync(key, basicDataApiType, domain);
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
}