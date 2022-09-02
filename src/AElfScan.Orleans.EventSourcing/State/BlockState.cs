using AElfScan.EventData;

namespace AElfScan.State;

public class BlockState
{
    public int BlockCount { get; set; }
    
    public long MaxBlockNumber { get; set; }
    public void Apply(BlockEventData data)
    {
        BlockCount += 1;
        if (MaxBlockNumber < data.BlockNumber)
        {
            MaxBlockNumber = data.BlockNumber;
        }
    }
}