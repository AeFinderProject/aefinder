namespace AeFinder.Market;

public class BillingPlanDto
{
    public decimal MonthlyUnitPrice { get; set; }
    public int BillingCycleMonthCount { get; set; }
    public decimal PeriodicCost { get; set; }
    public decimal FirstMonthCost { get; set; }
}