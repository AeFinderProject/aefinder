using System.Collections.Generic;

namespace AElfIndexer.Block.Dtos;

public class GetSubscriptionTransactionsInput
{
    public string ChainId { get; set; }
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
    public bool IsOnlyConfirmed { get; set; } = false;
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