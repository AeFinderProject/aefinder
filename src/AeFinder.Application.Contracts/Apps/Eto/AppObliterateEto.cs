using Orleans;
using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppObliterateEto")]
[GenerateSerializer]
public class AppObliterateEto
{
    [Id(0)] public string AppId { get; set; }
}