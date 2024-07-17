using Orleans;

namespace AeFinder.Subscriptions;

[GenerateSerializer]
public class AddSubscriptionDto
{
    [Id(0)]public string NewVersion { get; set; }
    [Id(1)]public string StopVersion { get; set; }
}