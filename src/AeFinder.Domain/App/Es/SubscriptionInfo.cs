using System.Collections.Generic;
using Nest;

namespace AeFinder.App.Es;

public class SubscriptionInfo
{
    [Keyword] public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public bool OnlyConfirmed { get; set; }
    public List<TransactionConditionInfo> TransactionConditions { get; set; } = new();
    public List<LogEventConditionInfo> LogEventConditions { get; set; } = new();
}