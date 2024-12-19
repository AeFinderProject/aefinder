namespace AeFinder.Grains.State.Market;

[GenerateSerializer]
public class OrderState
{
    [Id(0)]public string OrderId { get; set; }
    [Id(1)]public string OrganizationId { get; set; }
    [Id(2)]public string UserId { get; set; }
    [Id(3)]public string AppId { get; set; }
    [Id(4)]public string ProductId { get; set; }
    [Id(5)]public string ProductName { get; set; }
    [Id(6)]public ProductType ProductType { get; set; }
    [Id(7)]public int ProductNumber { get; set; }
    [Id(8)]public decimal UnitPrice { get; set; }
    [Id(9)]public DateTime OrderDate { get; set; }
    [Id(10)]public RenewalPeriod RenewalPeriod { get; set; }
    [Id(11)]public decimal OrderAmount { get; set; }
    [Id(12)]public OrderStatus OrderStatus { get; set; }
    [Id(13)]public bool EnableAutoRenewal { get; set; }
}