using AeFinder.Billings;
using AeFinder.Grains.State.Merchandises;

namespace AeFinder.Grains.State.Billings;

[GenerateSerializer]
public class BillingState
{
    [Id(0)]public Guid Id { get; set; }
    [Id(1)]public Guid OrganizationId { get; set; }
    [Id(2)]public DateTime BeginTime { get; set; }
    [Id(3)]public DateTime EndTime { get; set; }
    [Id(4)]public BillingType Type { get; set; }
    [Id(5)]public List<BillingDetailState> Details { get; set; }
    [Id(6)]public decimal RefundAmount { get; set; }
    [Id(7)]public decimal PaidAmount { get; set; }
    [Id(8)]public BillingStatus Status  { get; set; }
    [Id(9)]public string TransactionId { get; set; }
    [Id(10)]public DateTime CreateTime { get; set; }
    [Id(11)]public DateTime PaymentTime { get; set; }
}

[GenerateSerializer]
public class BillingDetailState
{
    [Id(0)]public MerchandiseState Merchandise { get; set; }
    [Id(1)]public string AppId { get; set; }
    [Id(2)]public int Quantity { get; set; }
    [Id(3)]public int Replicas { get; set; }
    [Id(4)]public decimal RefundAmount { get; set; }
    [Id(5)]public decimal PaidAmount { get; set; }
}