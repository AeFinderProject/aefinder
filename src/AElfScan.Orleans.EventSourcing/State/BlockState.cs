using AElfScan.EventData;

namespace AElfScan.State;

public class BlockState
{
    public int BlockCount { get; set; }
    public void Apply(BlockEventData data)
    {
        BlockCount += 1;
    }
}