using System.Collections.Generic;
using AeFinder.BlockScan;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps;

[EventName("AeFinder.AppUpgradeEto")]
[GenerateSerializer]
public class AppUpgradeEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string PendingVersion { get; set; }
    [Id(2)] public string CurrentVersion { get; set; }
    [Id(3)] public List<string> CurrentVersionChainIds { get; set; } 
}

[EventName("AeFinder.AppCurrentVersionSetEto")]
[GenerateSerializer]
public class AppCurrentVersionSetEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string CurrentVersion { get; set; }
}