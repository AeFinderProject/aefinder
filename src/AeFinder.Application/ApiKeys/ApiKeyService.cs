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
    private readonly IClusterClient _clusterClient;

    public ApiKeyService(IApiKeyTrafficProvider apiKeyTrafficProvider, IClusterClient clusterClient)
    {
        _apiKeyTrafficProvider = apiKeyTrafficProvider;
        _clusterClient = clusterClient;
    }

    public async Task IncreaseQueryAeIndexerCountAsync(string key, string appId)
    {
        throw new NotImplementedException();
    }

    public async Task IncreaseQueryBasicDataCountAsync(string key, BasicDataApiType basicDataApiType)
    {
        throw new NotImplementedException();
    }
}