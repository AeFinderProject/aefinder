using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.State.BlockScan;

public class ClientState
{
    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }
    public Dictionary<string, VersionSubscription> VersionSubscriptions { get; set; } = new();
}

public class VersionSubscription
{
    public Subscription Subscription { get; set; }= new();
    public VersionStatus VersionStatus { get; set; }
}

public enum VersionStatus
{
    Initialized = 0,
    Started = 1,
    Paused = 2
}