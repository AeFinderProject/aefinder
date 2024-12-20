using System;

namespace AeFinder.Market;

public class InvoiceInfoDto
{
    public string BillingId { get; set; }
    public BillingType BillingType { get; set; }
    public DateTime BillingDate { get; set; }
    public string Description { get; set; }
    public DateTime BillingStartDate { get; set; }
    public DateTime BillingEndDate { get; set; }
    public decimal BillingAmount { get; set; }
    public BillingStatus BillingStatus { get; set; }
    public string TransactionState { get; set; }
}