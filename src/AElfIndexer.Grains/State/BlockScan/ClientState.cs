using AElfIndexer.BlockScan;

namespace AElfIndexer.Grains.State.BlockScan;

public class ClientState
{
    public string ClientId { get; set; }
    public Guid MessageStreamId { get; set; }

    //public Dictionary<string, HashSet<string>> BlockScanIds { get; set; }

    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }

    public List<SubscribeInfo> SubscribeInfos { get; set; }
}