using System;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppDeleteEto")]
[GenerateSerializer]
public class AppDeleteEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public AppStatus Status { get; set; }
    [Id(2)] public DateTime DeleteTime { get; set; }
    [Id(3)] public string OrganizationId { get; set; }
}