using AeFinder.Studio;

namespace AeFinder.Grains.Grain.Apps;

public class AppInfo
{
    public HashSet<string> DeveloperIds { get; set; } = new();

    public Dictionary<string, AeFinderAppInfo> NameToApps { get; set; } = new();
}