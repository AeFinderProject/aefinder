using AElfIndexer.Studio;

namespace AElfIndexer.Grains.Grain.Apps;

public class UserAppGrainState
{
    public Dictionary<string, AeFinderAppInfo> NameToApps { get; set; } = new();
}