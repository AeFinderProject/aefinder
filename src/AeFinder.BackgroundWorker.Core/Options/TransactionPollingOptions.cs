namespace AeFinder.BackgroundWorker.Options;

public class TransactionPollingOptions
{
    public int DelaySeconds { get; set; } = 3;
    public int RetryTimes { get; set; } = 10;
}