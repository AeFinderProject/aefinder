using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.State.BlockScan;

public class ClientState
{
    public string ClientId { get; set; }
    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }
    public Dictionary<string, ClientVersionInfo> VersionInfos { get; set; }
}

public class ClientVersionInfo
{
    public HashSet<string> BlockScanIds { get; set; }
    public List<SubscribeInfo> SubscribeInfos { get; set; }
    public VersionStatus VersionStatus { get; set; }
}

public enum VersionStatus
{
    Initialized = 0,
    Started = 1,
    Stopped = 2
}