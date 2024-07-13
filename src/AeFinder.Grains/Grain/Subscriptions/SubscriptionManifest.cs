namespace AeFinder.Grains.Grain.Subscriptions;

[GenerateSerializer]
public class SubscriptionManifest
{
    [Id(0)]public List<Subscription> SubscriptionItems { get; set; } = new();
}

[GenerateSerializer]
public class Subscription
{
    [Id(0)]public string ChainId { get; set; }
    [Id(1)]public long StartBlockNumber { get; set; }
    [Id(2)]public bool OnlyConfirmed { get; set; }
    [Id(3)]public List<TransactionCondition> TransactionConditions { get; set; } = new();
    [Id(4)]public List<LogEventCondition> LogEventConditions { get; set; } = new();
}

[GenerateSerializer]
public class TransactionCondition
{
    [Id(0)]public string To { get; set; }
    [Id(1)]public List<string> MethodNames { get; set; } = new();
}

[GenerateSerializer]
public class LogEventCondition
{
    [Id(0)]public string ContractAddress { get; set; }
    [Id(1)]public List<string> EventNames { get; set; } = new();
}