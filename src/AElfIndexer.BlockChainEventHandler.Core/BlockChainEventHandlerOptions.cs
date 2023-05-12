namespace AElfIndexer;

public class BlockChainEventHandlerOptions
{
    public int BlockPartionLimit { get; set; } = 100;
    public int RetryTimes { get; set; } = 5;
    public int RetryInterval { get; set; } = 10000;
}