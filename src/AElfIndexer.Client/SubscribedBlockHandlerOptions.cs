namespace AElfIndexer.Client;

public class SubscribedBlockHandlerOptions
{
    public int RetryTimes { get; set; } = 5;
    public int RetryInterval { get; set; } = 10000;
}