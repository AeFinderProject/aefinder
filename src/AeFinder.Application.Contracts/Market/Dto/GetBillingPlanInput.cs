namespace AeFinder.Market;

public class GetBillingPlanInput
{
    public string ProductId { get; set; }
    public int ProductNum { get; set; }
    public int PeriodMonths { get; set; }
}