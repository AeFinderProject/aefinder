using System.Collections.Generic;

namespace AElfIndexer.BlockSync;

public class BlockSyncOptions
{
    public Dictionary<string, long> FastSyncEndHeight { get; set; }
}