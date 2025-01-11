using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Merchandises;

namespace AeFinder.Grains.Grain.Orders;

public class OrderCost
{
    public List<OrderCostDetail> Details { get; set; } = new();
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
}

public class OrderCostDetail
{
    public AssetState OriginalAsset { get; set; }
    public MerchandiseState Merchandise { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
}