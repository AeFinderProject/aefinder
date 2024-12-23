namespace AeFinder.Market;

public class PendingBillDto
{
    public string BillingId { get; set; }
    public BillingType BillingType { get; set; }
    public decimal BillingAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public BillingStatus BillingStatus { get; set; }
    public string TransactionState { get; set; }
    public ProductType ProductType { get; set; }
}