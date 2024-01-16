using AeFinder.Grains.EventData;

namespace AeFinder.Grains.State.Blocks;

public class BlockBranchState
{
    // public Dictionary<string, BlockData> Blocks = new Dictionary<string, BlockData>();
    public Dictionary<string, BlockBasicData> Blocks = new Dictionary<string, BlockBasicData>();
}