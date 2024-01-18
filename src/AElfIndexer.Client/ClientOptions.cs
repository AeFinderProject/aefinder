namespace AElfIndexer.Client;

public class ClientOptions
{
    public int AppDataCacheCount { get; set; } = 1000;
    public ClientType ClientType { get; set; } = ClientType.Full;
    public MessageQueueOptions MessageQueue { get; set; } = new();
}

public enum ClientType
{
    Full, // Provide data processing and query capabilities
    Query // Provide only query capability
}

public class MessageQueueOptions
{
    public int RetryTimes { get; set; } = 5;
    public int RetryInterval { get; set; } = 10000;
}