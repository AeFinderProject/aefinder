using AElfIndexer.Grains.EventData;

namespace AElfIndexer.Grains.State.Blocks;

public class BlockDictionaryState
{
    public Dictionary<string, BlockEventData> Blocks = new Dictionary<string, BlockEventData>();
}