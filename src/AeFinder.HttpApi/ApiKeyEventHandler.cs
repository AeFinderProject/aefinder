using System.Threading.Tasks;
using AeFinder.ApiKeys;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder;

public class ApiKeyEventHandler : 
    IDistributedEventHandler<ApiKeyChangedEto>, 
    IDistributedEventHandler<ApiKeySummaryChangedEto>, 
    IDistributedEventHandler<ApiKeySummarySnapshotChangedEto>, 
    ITransientDependency
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IObjectMapper _objectMapper;
    private readonly IClock _clock;

    public ApiKeyEventHandler(IApiKeyService apiKeyService, IObjectMapper objectMapper, IClock clock)
    {
        _apiKeyService = apiKeyService;
        _objectMapper = objectMapper;
        _clock = clock;
    }

    public async Task HandleEventAsync(ApiKeyChangedEto eventData)
    {
        var info = _objectMapper.Map<ApiKeyChangedEto, ApiKeyInfo>(eventData);
        await _apiKeyService.UpdateApiKeyInfoCacheAsync(info);
    }

    public async Task HandleEventAsync(ApiKeySummaryChangedEto eventData)
    {
        await _apiKeyService.UpdateApiKeyLimitCacheAsync(eventData.OrganizationId, eventData.QueryLimit);
    }

    public async Task HandleEventAsync(ApiKeySummarySnapshotChangedEto eventData)
    {
        
        if (eventData.Type == SnapshotType.Monthly && eventData.Time.Year == _clock.Now.Year && eventData.Time.Month == _clock.Now.Month)
        {
            await _apiKeyService.UpdateApiKeyUsedCacheAsync(eventData.OrganizationId, eventData.Query);
        }
    }
}