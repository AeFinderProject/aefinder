using AeFinder.Assets;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppAssetChangeHandler: IDistributedEventHandler<AppAssetChangedEto>, ITransientDependency
{
    private readonly ILogger<AppAssetChangeHandler> _logger;
    private readonly IAssetService _assetService;

    public AppAssetChangeHandler(ILogger<AppAssetChangeHandler> logger,
        IAssetService assetService)
    {
        _logger = logger;
        _assetService = assetService;
    }
    
    public async Task HandleEventAsync(AppAssetChangedEto eventData)
    {
        
    }
}