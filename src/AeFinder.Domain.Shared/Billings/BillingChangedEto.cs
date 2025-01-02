using System;
using System.Collections.Generic;
using AeFinder.Merchandises;

namespace AeFinder.Billings;

public class BillingChangedEto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    public BillingType Type { get; set; }
    public List<BillingDetailChangedEto> Details { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public BillingStatus Status  { get; set; }
    public string TransactionId { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime PaymentTime { get; set; }
}

public class BillingDetailChangedEto
{
    public MerchandiseChangedEto Merchandise { get; set; }
    public string AppId { get; set; }
    public int Quantity { get; set; }
    public int Replicas { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PaidAmount { get; set; }
}