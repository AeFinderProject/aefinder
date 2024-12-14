namespace AeFinder.Grains.State.Market;

[GenerateSerializer]
public class BillState
{
    [Id(0)]public string BillingId { get; set; }
    [Id(1)]public string OrganizationId { get; set; }
    [Id(2)]public string OrderId { get; set; }
    [Id(3)]public string SubscriptionId { get; set; }
    [Id(4)]public string UserId { get; set; }
    [Id(5)]public string AppId { get; set; }
    [Id(6)]public BillingType BillingType { get; set; }
    [Id(7)]public DateTime BillingDate { get; set; }
    [Id(8)]public string Description { get; set; }
    [Id(9)]public string TransactionId { get; set; }
    [Id(10)]public decimal TransactionAmount { get; set; }
    [Id(11)]public string WalletAddress { get; set; }
    [Id(12)]public DateTime BillingStartDate { get; set; }
    [Id(13)]public DateTime BillingEndDate { get; set; }
    [Id(14)]public decimal BillingAmount { get; set; }
    [Id(15)]public decimal RefundAmount { get; set; }
    [Id(16)]public BillingStatus BillingStatus { get; set; }
}