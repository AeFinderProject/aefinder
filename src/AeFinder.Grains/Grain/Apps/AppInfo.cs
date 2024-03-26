using AeFinder.Studio;

namespace AeFinder.Grains.Grain.Apps;

public class AppInfo
{
    public string AppId { get; set; }
    public HashSet<string> DeveloperIds { get; set; } = new();

    public AeFinderAppInfo AeFinderAppInfo { get; set; } = new();
}