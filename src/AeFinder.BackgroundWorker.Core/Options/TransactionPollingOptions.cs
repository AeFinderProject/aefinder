namespace AeFinder.BackgroundWorker.Options;

public class TransactionPollingOptions
{
    public int DelaySeconds { get; set; }
    public int RetryTimes { get; set; }
}