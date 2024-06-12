namespace AeFinder.Grains.Grain.Subscriptions;

public class SubscriptionManifest
{
    public List<Subscription> SubscriptionItems { get; set; } = new();
}

public class Subscription
{
    public string ChainId { get; set; }
    public long StartBlockNumber { get; set; }
    public bool OnlyConfirmed { get; set; }
    public List<TransactionCondition> TransactionConditions { get; set; } = new();
    public List<LogEventCondition> LogEventConditions { get; set; } = new();
}

public class TransactionCondition
{
    public string To { get; set; }
    public List<string> MethodNames { get; set; } = new();
}

public class LogEventCondition
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; } = new();
}