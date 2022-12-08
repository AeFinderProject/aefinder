using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Handlers;

public class TransactionInfo : TransactionBase
{
    public List<LogEventInfo> LogEvents { get; set; }
}