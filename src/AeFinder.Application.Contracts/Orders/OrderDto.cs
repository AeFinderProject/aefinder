using System;
using System.Collections.Generic;
using AeFinder.Assets;
using AeFinder.Merchandises;

namespace AeFinder.Orders;

public class OrderDto
{
    public string Id  { get; set; }
    public Guid OrganizationId { get; set; }
    public List<OrderDetail> Details  { get; set; }
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public OrderStatus Status  { get; set; }
    public PaymentType PaymentType  { get; set; }
    public DateTime OrderTime  { get; set; }
    public DateTime PaymentTime { get; set; }
    public Dictionary<string,string> ExtraData { get; set; } = new();
    public Guid UserId { get; set; }
    public string TransactionId { get; set; }
}

public class OrderDetail
{
    public AssetDto OriginalAsset { get; set; }
    public MerchandiseDto Merchandise { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public decimal Amount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal ActualAmount { get; set; }
}