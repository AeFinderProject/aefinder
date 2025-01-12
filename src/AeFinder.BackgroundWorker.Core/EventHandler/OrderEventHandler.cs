using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.Orders;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class OrderEventHandler :
    IDistributedEventHandler<OrderChangedEto>,
    IDistributedEventHandler<OrderStatusChangedEto>,
    ITransientDependency
{
    private readonly IOrderService _orderService;
    private readonly IAssetService _assetService;
    private readonly IAppService _appService;

    public OrderEventHandler(IOrderService orderService, IAssetService assetService, IAppService appService)
    {
        _orderService = orderService;
        _assetService = assetService;
        _appService = appService;
    }

    public async Task HandleEventAsync(OrderChangedEto eventData)
    {
        await _orderService.AddOrUpdateIndexAsync(eventData);
    }

    public async Task HandleEventAsync(OrderStatusChangedEto eventData)
    {
        switch (eventData.Status)
        {
            // case OrderStatus.Unpaid:
            //     foreach (var detail in eventData.Details)
            //     {
            //         if (detail.OriginalAsset != null)
            //         {
            //             await _assetService.LockAsync(detail.OriginalAsset.Id, true);
            //         }
            //     }
            //     await LockAppAsync(eventData, true);
            //     break;
            case OrderStatus.Paid:
                await _assetService.ChangeAssetAsync(eventData);
                await LockAppAsync(eventData, false);
                break;
            case OrderStatus.Canceled:
                foreach (var detail in eventData.Details)
                {
                    if (detail.OriginalAsset != null)
                    {
                        await _assetService.LockAsync(detail.OriginalAsset.Id, false);
                    }
                }
                await LockAppAsync(eventData, false);
                break;
        }
    }

    private async Task LockAppAsync(OrderStatusChangedEto eventData, bool isLock)
    {
        if (eventData.ExtraData.TryGetValue(AeFinderApplicationConsts.RelateAppExtraDataKey, out var appId))
        {
            await _appService.LockAsync(appId, isLock);
        }
    }
}