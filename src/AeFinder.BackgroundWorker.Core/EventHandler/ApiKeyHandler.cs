using AeFinder.ApiKeys;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class ApiKeyHandler : 
    IDistributedEventHandler<ApiKeyChangedEto>, 
    IDistributedEventHandler<ApiKeyQueryAeIndexerChangedEto>, 
    IDistributedEventHandler<ApiKeyQueryAeIndexerSnapshotChangedEto>, 
    IDistributedEventHandler<ApiKeyQueryBasicApiChangedEto>, 
    IDistributedEventHandler<ApiKeyQueryBasicApiSnapshotChangedEto>, 
    IDistributedEventHandler<ApiKeySnapshotChangedEto>, 
    IDistributedEventHandler<ApiKeySummaryChangedEto>, 
    IDistributedEventHandler<ApiKeySummarySnapshotChangedEto>, 
    ITransientDependency
{

    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeySnapshotService _apiKeySnapshotService;

    public ApiKeyHandler(IApiKeyService apiKeyService, IApiKeySnapshotService apiKeySnapshotService)
    {
        _apiKeyService = apiKeyService;
        _apiKeySnapshotService = apiKeySnapshotService;
    }

    public async Task HandleEventAsync(ApiKeyChangedEto eventData)
    {
        await _apiKeyService.AddOrUpdateApiKeyIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeyQueryAeIndexerChangedEto eventData)
    {
        await _apiKeyService.AddOrUpdateApiKeyQueryAeIndexerIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeyQueryAeIndexerSnapshotChangedEto eventData)
    {
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeyQueryBasicApiChangedEto eventData)
    {
        await _apiKeyService.AddOrUpdateApiKeyQueryBasicApiIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeyQueryBasicApiSnapshotChangedEto eventData)
    {
        await _apiKeySnapshotService.AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeySnapshotChangedEto eventData)
    {
        await _apiKeySnapshotService.AddOrUpdateApiKeySnapshotIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeySummaryChangedEto eventData)
    {
        await _apiKeyService.AddOrUpdateApiKeySummaryIndexAsync(eventData);
    }

    public async Task HandleEventAsync(ApiKeySummarySnapshotChangedEto eventData)
    {
        await _apiKeySnapshotService.AddOrUpdateApiKeySummarySnapshotIndexAsync(eventData);
    }
}