namespace AeFinder.Billings;

public class BillingOptions
{
    public int BillingOverdueMinutes { get; set; } = 10080;
    public int TransactionTimeoutMinutes { get; set; } = 60;
    public int PaymentWaitingMinutes { get; set; } = 10;
    public int PayFailedBillingCheckMonth { get; set; } = -1;
}