using AeFinder.Assets;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AssetEventHandler: 
    IDistributedEventHandler<AssetChangedEto>, 
    ITransientDependency
{
    private readonly IAssetService _assetService;

    public AssetEventHandler(IAssetService assetService)
    {
        _assetService = assetService;
    }

    public async Task HandleEventAsync(AssetChangedEto eventData)
    {
        await _assetService.AddOrUpdateIndexAsync(eventData);
    }
}