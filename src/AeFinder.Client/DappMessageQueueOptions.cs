namespace AeFinder.Client;

public class DappMessageQueueOptions
{
    public int RetryTimes { get; set; } = 5;
    public int RetryInterval { get; set; } = 10000;
}