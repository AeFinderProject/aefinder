namespace AeFinder.Grains.State.Market;

public class RenewalState
{
    public string SubscriptionId { get; set; }
    public string OrganizationId { get; set; }
    public string UserId { get; set; }
    public string AppId { get; set; }
    public string ProductId { get; set; }
    public ProductType ProductType { get; set; }
    public int ProductNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime LastChargeDate { get; set; }
    public DateTime NextRenewalDate { get; set; }
    public RenewalPeriod RenewalPeriod { get; set; }
    public decimal PeriodicCost { get; set; }
    public bool IsActive { get; set; }
}