using System;
using AeFinder.BlockScan;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppSubscriptionCreateEto")]
[GenerateSerializer]
public class AppSubscriptionCreateEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string CurrentVersion { get; set; }
    [Id(2)] public string PendingVersion { get; set; }
}