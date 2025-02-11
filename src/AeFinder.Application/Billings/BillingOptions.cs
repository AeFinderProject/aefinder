namespace AeFinder.Billings;

public class BillingOptions
{
    public int BillingOverdueDays { get; set; } = 7;
    public int TransactionTimeoutMinutes { get; set; } = 60;
    public int PaymentWaitingMinutes { get; set; } = 10;
}