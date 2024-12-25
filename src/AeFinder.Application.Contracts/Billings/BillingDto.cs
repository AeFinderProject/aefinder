using System;
using System.Collections.Generic;
using AeFinder.Merchandises;

namespace AeFinder.Billings;

public class BillingDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    public BillingType Type { get; set; }
    public List<BillingDetailDto> Details { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public BillingStatus Status  { get; set; }
    public string TransactionId { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime PaymentTime { get; set; }
}

public class BillingDetailDto
{
    public string MerchandiseName { get; set; }
    public string AppId { get; set; }
    public MerchandiseType Type { get; set; }
    public string Unit { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int Duration { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
}