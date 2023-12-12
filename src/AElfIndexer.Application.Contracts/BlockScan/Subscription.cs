using System.Collections.Generic;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.BlockScan;

public class Subscription
{
    // chain id -> subscription
    public Dictionary<string, SubscriptionItem> Items { get; set; }
}

public class SubscriptionItem
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public bool OnlyConfirmed { get; set; }
    public List<TransactionFilter> Transaction { get; set; }
    public List<LogEventFilter> LogEvent { get; set; }
}