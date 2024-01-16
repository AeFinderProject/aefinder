namespace AeFinder.Grains.State.Client;

public class TransactionInfo : TransactionBase
{
    public List<LogEventInfo> LogEvents { get; set; }
}