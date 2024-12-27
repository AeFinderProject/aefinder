using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Merchandises;
using AeFinder.Orders;

namespace AeFinder.Grains.State.Orders;

[GenerateSerializer]
public class OrderState
{
    [Id(0)]public Guid Id  { get; set; }
    [Id(1)]public Guid OrganizationId { get; set; }
    [Id(2)] public List<OrderDetailState> Details { get; set; } = new();
    [Id(3)]public decimal Amount { get; set; }
    [Id(4)]public decimal DeductionAmount { get; set; }
    [Id(5)]public decimal ActualAmount { get; set; }
    [Id(6)]public OrderStatus Status  { get; set; }
    [Id(7)]public PaymentType PaymentType  { get; set; }
    [Id(8)]public DateTime OrderTime  { get; set; }
    [Id(9)]public DateTime PaymentTime { get; set; }
    [Id(10)]public Dictionary<string,string> ExtraData { get; set; } = new();
    [Id(11)]public Guid UserId { get; set; }
    [Id(12)]public string TransactionId { get; set; }
}

[GenerateSerializer]
public class OrderDetailState
{
    [Id(0)]public AssetState OriginalAsset { get; set; }
    [Id(1)]public MerchandiseState Merchandise { get; set; }
    [Id(2)]public long Quantity { get; set; }
    [Id(3)]public long Replicas { get; set; }
    [Id(4)]public decimal Amount { get; set; }
    [Id(5)]public decimal DeductionAmount { get; set; }
    [Id(6)]public decimal ActualAmount { get; set; }
}