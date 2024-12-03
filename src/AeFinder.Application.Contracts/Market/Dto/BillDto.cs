using System;

namespace AeFinder.Market;

public class BillDto
{
    public string BillingId { get; set; }
    public string OrganizationId { get; set; }
    public string OrderId { get; set; }
    public string SubscriptionId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public BillingType BillingType { get; set; }
    public DateTime BillingDate { get; set; }
    public string Description { get; set; }
    public string TransactionId { get; set; }
    public decimal TransactionAmount { get; set; }
    public string WalletAddress { get; set; }
    public DateTime BillingStartDate { get; set; }
    public DateTime BillingEndDate { get; set; }
    public decimal BillingAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public BillingStatus BillingStatus { get; set; }
}