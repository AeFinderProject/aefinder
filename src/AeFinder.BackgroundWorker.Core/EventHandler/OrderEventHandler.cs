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

    public OrderEventHandler(IOrderService orderService, IAssetService assetService)
    {
        _orderService = orderService;
        _assetService = assetService;
    }

    public async Task HandleEventAsync(OrderChangedEto eventData)
    {
        await _orderService.AddOrUpdateIndexAsync(eventData);
    }

    public async Task HandleEventAsync(OrderStatusChangedEto eventData)
    {
        switch (eventData.Status)
        {
            case OrderStatus.Unpaid:
                foreach (var detail in eventData.Details)
                {
                    if (detail.OriginalAsset != null)
                    {
                        await _assetService.LockAsync(detail.OriginalAsset.Id, true);
                    }
                }
                break;
            case OrderStatus.Paid:
                await _assetService.ChangeAssetAsync(eventData);
                break;
            case OrderStatus.Canceled:
                foreach (var detail in eventData.Details)
                {
                    if (detail.OriginalAsset != null)
                    {
                        await _assetService.LockAsync(detail.OriginalAsset.Id, false);
                    }
                }
                break;
        }
    }
}