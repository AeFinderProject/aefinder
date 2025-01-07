using AeFinder.Assets;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using AeFinder.Orders;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Grains.Grain.Orders;

public class OrderCostProvider : IOrderCostProvider, ITransientDependency
{
    private readonly IClusterClient _clusterClient;

    public OrderCostProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<OrderCost> CalculateCostAsync(List<CreateOrderDetail> details, DateTime orderTime,
        DateTime endTime)
    {
        var orderCost = new OrderCost();
        foreach (var inputDetail in details)
        {
            var merchandise = await _clusterClient.GetGrain<IMerchandiseGrain>(inputDetail.MerchandiseId).GetAsync();
            var orderDetail = new OrderCostDetail();
            var freeQuantity = 0L;
            if (inputDetail.OriginalAssetId.HasValue)
            {
                var asset = await _clusterClient.GetGrain<IAssetGrain>(inputDetail.OriginalAssetId.Value).GetAsync();
                orderDetail.OriginalAsset = asset;
                
                var originalMerchandise = await _clusterClient.GetGrain<IMerchandiseGrain>(asset.MerchandiseId).GetAsync();
                if (originalMerchandise.Type != merchandise.Type)
                {
                    throw new UserFriendlyException("Wrong merchandise type.");
                }

                if (asset.Quantity * asset.Replicas - asset.FreeQuantity * asset.FreeReplicas != 0)
                {
                    var deductionQuantity = 0L;
                    switch (merchandise.ChargeType)
                    {
                        case ChargeType.Hourly:
                            deductionQuantity = (long)Math.Floor((endTime - orderTime).TotalHours) *
                                                asset.Replicas;
                            orderDetail.DeductionAmount = deductionQuantity * merchandise.Price;
                            break;
                        case ChargeType.Time:
                            deductionQuantity = asset.Quantity * asset.Replicas -
                                                asset.FreeQuantity * asset.FreeReplicas;
                            orderDetail.DeductionAmount = deductionQuantity * merchandise.Price;
                            break;
                    }

                    orderCost.DeductionAmount += orderDetail.DeductionAmount;
                }

                if (asset.FreeType == AssetFreeType.Permanent)
                {
                    freeQuantity = asset.FreeQuantity * asset.FreeReplicas;
                }
            }

            orderDetail.Merchandise = merchandise;
            switch (merchandise.ChargeType)
            {
                case ChargeType.Hourly:
                    orderDetail.Quantity = (long)Math.Ceiling((endTime - orderTime).TotalHours);
                    break;
                case ChargeType.Time:
                    orderDetail.Quantity = inputDetail.Quantity;
                    break;
            }

            orderDetail.Replicas = inputDetail.Replicas;
            orderDetail.Amount = (orderDetail.Quantity * orderDetail.Replicas - freeQuantity) * merchandise.Price;
            orderDetail.ActualAmount = orderDetail.Amount - orderDetail.DeductionAmount;
            if (orderDetail.ActualAmount < 0)
            {
                orderDetail.ActualAmount = 0;
            }

            orderCost.Details.Add(orderDetail);
            orderCost.Amount += orderDetail.Amount;
            orderCost.ActualAmount += orderDetail.ActualAmount;
        }

        return orderCost;
    }
}