using System;
using AeFinder.BlockScan;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppSubscriptionCreateEto")]
public class AppSubscriptionCreateEto
{
    public string AppId { get; set; }
    public AppVersionInfoEto CurrentVersion { get; set; }
    public AppVersionInfoEto PendingVersion { get; set; }
}

public class AppVersionInfoEto
{
    public string Version { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public string DockerImage { get; set; }
}