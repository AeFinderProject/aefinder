using System;
using System.Collections.Generic;
using AeFinder.Assets;
using AeFinder.Merchandises;

namespace AeFinder.Billings;

public class BillingDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    public BillingType Type { get; set; }
    public List<BillingDetailDto> Details { get; set; } = new();
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public BillingStatus Status  { get; set; }
    public string TransactionId { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime PaymentTime { get; set; }
}

public class BillingDetailDto
{
    public MerchandiseDto Merchandise { get; set; }
    public AssetDto Asset { get; set; }
    public long Quantity { get; set; }
    public long Replicas { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
}