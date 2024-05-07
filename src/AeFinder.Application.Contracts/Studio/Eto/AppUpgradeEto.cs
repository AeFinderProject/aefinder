using Volo.Abp.EventBus;

namespace AeFinder.Studio.Eto;

[EventName("AppUpgradeEto")]
public class AppUpgradeEto
{
    public string AppId { get; set; }
    public string NewVersion { get; set; }
    public string CurrentVersion { get; set; }
}

[EventName("AppCurrentVersionSetEto")]
public class AppCurrentVersionSetEto
{
    public string AppId { get; set; }
    public string CurrentVersion { get; set; }
}