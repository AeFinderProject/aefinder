namespace AeFinder.Market;

public class CreateRefundBillDto
{
    public string OrganizationId { get; set; }
    public string OrderId { get; set; }
    public string SubscriptionId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string Description { get; set; }
    public decimal RefundFee { get; set; }
}