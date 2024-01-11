using System.Collections.Generic;

namespace AElfIndexer.BlockScan;

public class SubscriptionDto
{
    public List<SubscriptionItemDto> SubscriptionItems { get; set; }
}

public class SubscriptionItemDto
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public bool OnlyConfirmed { get; set; }
    public List<TransactionConditionDto> TransactionConditions { get; set; } = new();
    public List<LogEventConditionDto> LogEventConditions { get; set; } = new();
}

public class TransactionConditionDto
{
    public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}

public class LogEventConditionDto
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}