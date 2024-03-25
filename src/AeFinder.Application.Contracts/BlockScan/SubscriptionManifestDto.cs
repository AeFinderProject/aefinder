using System.Collections.Generic;

namespace AeFinder.BlockScan;

public class SubscriptionManifestDto
{
    public List<SubscriptionDto> SubscriptionItems { get; set; }
}

public class SubscriptionDto
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