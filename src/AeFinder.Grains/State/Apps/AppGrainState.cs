using AeFinder.Studio;

namespace AeFinder.Grains.State.Apps;

public class AppGrainState
{
    public string AdminId { get; set; }
    public string AppId { get; set; }
    public string Name { get; set; }

    public HashSet<string> DeveloperIds { get; set; } = new();

    public Dictionary<string, AeFinderAppInfo> NameToApps { get; set; } = new();
}