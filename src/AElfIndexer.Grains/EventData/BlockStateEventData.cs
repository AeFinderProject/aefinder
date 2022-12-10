using System;

namespace AElfIndexer.Grains.EventData;

[Serializable]
public class BlockStateEventData
{
    public string BlockHash { get; set; }
    public BlockData BlockInfo { get; set; }
}