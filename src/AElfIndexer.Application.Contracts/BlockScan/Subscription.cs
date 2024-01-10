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
    public List<TransactionFilter> TransactionFilters { get; set; } = new();
    public List<LogEventFilter> LogEventFilters { get; set; } = new();
}

public class TransactionFilter
{
    public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}

public class LogEventFilter
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}