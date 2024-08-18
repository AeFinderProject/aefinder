using System;
using Orleans;

namespace AeFinder.Apps;

[GenerateSerializer]
public class AppDto
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string DeployKey { get; set; }
    [Id(2)] public string AppName { get; set; }
    [Id(3)] public string ImageUrl { get; set; }
    [Id(4)] public string Description { get; set; }
    [Id(5)] public string SourceCodeUrl { get; set; }
    [Id(6)] public AppStatus Status { get; set; }
    [Id(7)] public long CreateTime { get; set; }
    [Id(8)] public long UpdateTime { get; set; }
    [Id(9)] public AppVersion Versions { get; set; } = new();
    [Id(10)] public string OrganizationId { get; set; }
    [Id(11)] public string OrganizationName { get; set; }
}

[GenerateSerializer]
public class AppVersion
{
    [Id(0)] public string CurrentVersion { get; set; }
    [Id(1)] public string PendingVersion { get; set; }
}