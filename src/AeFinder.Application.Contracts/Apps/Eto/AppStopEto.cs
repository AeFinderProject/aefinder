using System.Collections.Generic;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppStopEto")]
[GenerateSerializer]
public class AppStopEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string StopVersion { get; set; }
    [Id(2)] public List<string> StopVersionChainIds { get; set; } 
}