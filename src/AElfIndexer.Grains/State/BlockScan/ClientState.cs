using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.State.BlockScan;

public class ClientState
{
    public string ClientId { get; set; }
    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }
    public Dictionary<string, ClientVersionInfo> VersionInfos { get; set; } = new();
}

public class ClientVersionInfo
{
    public HashSet<string> BlockScanIds { get; set; }= new();
    public List<SubscriptionInfo> SubscriptionInfos { get; set; }= new();
    public VersionStatus VersionStatus { get; set; }
}

public enum VersionStatus
{
    Initialized = 0,
    Started = 1
}

public class ClientVersion
{
    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }
}