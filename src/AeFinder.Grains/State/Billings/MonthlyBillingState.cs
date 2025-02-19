namespace AeFinder.Grains.State.Billings;

[GenerateSerializer]
public class MonthlyBillingState
{
    [Id(0)]
    public Guid OrganizationId { get; set; }
    [Id(1)]
    public DateTime BillingDate { get; set; }
    [Id(2)]
    public Guid SettlementBillingId { get; set; }
    [Id(3)]
    public Guid AdvancePaymentBillingId { get; set; }
}