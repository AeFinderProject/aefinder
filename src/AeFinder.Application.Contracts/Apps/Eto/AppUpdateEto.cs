using System;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppUpdateEto")]
[GenerateSerializer]
public class AppUpdateEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string ImageUrl { get; set; }
    [Id(2)] public string Description { get; set; }
    [Id(3)] public string SourceCodeUrl { get; set; }
    [Id(4)] public DateTime UpdateTime { get; set; }
    [Id(5)] public bool IsLocked { get; set; }
}