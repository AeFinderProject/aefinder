using System.Collections.Generic;

namespace AElfIndexer.Grains.State.BlockScan;

public class BlockScanExecutorState
{
    public long ScannedBlockHeight { get; set; }
    public string ScannedBlockHash { get; set; }
    public long ScannedConfirmedBlockHeight { get; set; }
    public string ScannedConfirmedBlockHash { get; set; }
    public SortedDictionary<long, HashSet<string>> ScannedBlocks = new();
    public string ScanToken { get; set; }
}