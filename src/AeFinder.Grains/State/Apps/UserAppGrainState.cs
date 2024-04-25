using AeFinder.Studio;

namespace AeFinder.Grains.Grain.Apps;

public class UserAppGrainState
{
    public Dictionary<string, AeFinderAppInfo> NameToApps { get; set; } = new();
}