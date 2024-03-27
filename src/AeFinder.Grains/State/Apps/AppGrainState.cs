using AeFinder.Studio;

namespace AeFinder.Grains.State.Apps;

public class AppGrainState
{
    public string AdminId { get; set; }
    public string AppId { get; set; }
    
    //duplicated, appid is name
    public string Name { get; set; }

    public HashSet<string> DeveloperIds { get; set; } = new();

    public AeFinderAppInfo AeFinderAppInfo { get; set; } = new();

    public Dictionary<string, string> VersionToGraphQl { get; set; } = new();
}