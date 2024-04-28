using System.Collections.Generic;

namespace AeFinder.BlockSync;

public class BlockSyncOptions
{
    public Dictionary<string, long> FastSyncEndHeight { get; set; } = new();
}