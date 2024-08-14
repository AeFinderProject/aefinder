using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppSubscriptionCreateEto")]
[GenerateSerializer]
public class AppSubscriptionUpdateEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string Version { get; set; }
}