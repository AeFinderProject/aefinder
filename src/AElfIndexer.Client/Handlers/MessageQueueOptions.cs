namespace AElfIndexer.Client.Handlers;

public class MessageQueueOptions
{
    public int RetryTimes { get; set; } = 5;
    public int RetryInterval { get; set; } = 10000;
}