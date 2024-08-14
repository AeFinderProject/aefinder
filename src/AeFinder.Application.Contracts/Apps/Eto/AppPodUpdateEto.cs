using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppPodUpdateEto")]
public class AppPodUpdateEto
{
    public string AppId { get; set; }
    public string Version { get; set; }
    public string DockerImage { get; set; }
}