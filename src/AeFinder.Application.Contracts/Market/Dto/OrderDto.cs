using System;

namespace AeFinder.Market;

public class OrderDto
{
    public string OrderId { get; set; }
    public string OrganizationId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public ProductType ProductType { get; set; }
    public int ProductNumber { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public RenewalPeriod RenewalPeriod { get; set; }
    public decimal OrderAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public bool EnableAutoRenewal { get; set; }
}