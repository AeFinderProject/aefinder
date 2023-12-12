using System.Collections.Generic;

namespace AElfIndexer.Grains.State.BlockScan;

public class BlockScanState
{
    // public string Version { get; set; }
    // public string ChainId { get; set; }
    // public string ClientId { get; set; }
    public Guid MessageStreamId { get; set; } 
    public long ScannedBlockHeight { get; set; }
    public string ScannedBlockHash { get; set; }
    public long ScannedConfirmedBlockHeight { get; set; }
    public string ScannedConfirmedBlockHash { get; set; }
    public SortedDictionary<long, HashSet<string>> ScannedBlocks = new();
    public string Token { get; set; }
}