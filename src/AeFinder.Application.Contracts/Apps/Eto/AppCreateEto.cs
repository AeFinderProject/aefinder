using System;
using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppCreateEto")]
[GenerateSerializer]
public class AppCreateEto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string OrganizationId { get; set; }
    [Id(2)] public string DeployKey { get; set; }
    [Id(3)] public string AppName { get; set; }
    [Id(4)] public string ImageUrl { get; set; }
    [Id(5)] public string Description { get; set; }
    [Id(6)] public string SourceCodeUrl { get; set; }
    [Id(7)] public DateTime CreateTime { get; set; }
    [Id(8)] public DateTime UpdateTime { get; set; }
    [Id(9)] public AppStatus Status { get; set; }
    
}