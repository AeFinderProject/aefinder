using Orleans;

namespace AeFinder.Apps;

[GenerateSerializer]
public class AppInfoImmutable
{
    [Id(0)]public string AppId { get; set; }
    [Id(1)]public string AppName { get; set; }
}