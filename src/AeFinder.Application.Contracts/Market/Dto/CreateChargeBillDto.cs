namespace AeFinder.Market;

public class CreateChargeBillDto
{
    public string OrganizationId { get; set; }
    public string OrderId { get; set; }
    public string SubscriptionId { get; set; }
    public string Description { get; set; }
    public decimal ChargeFee { get; set; }
    public decimal RefundAmount { get; set; }
}