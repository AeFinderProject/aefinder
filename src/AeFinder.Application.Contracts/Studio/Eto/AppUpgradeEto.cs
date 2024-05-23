using Volo.Abp.EventBus;

namespace AeFinder.Studio.Eto;

[EventName("AeFinder.AppUpgradeEto")]
public class AppUpgradeEto
{
    public string AppId { get; set; }
    public string PendingVersion { get; set; }
    public string CurrentVersion { get; set; }
}

[EventName("AeFinder.AppCurrentVersionSetEto")]
public class AppCurrentVersionSetEto
{
    public string AppId { get; set; }
    public string CurrentVersion { get; set; }
}