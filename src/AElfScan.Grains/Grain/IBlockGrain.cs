using AElfScan.Grains.EventData;
using Orleans;

namespace AElfScan.Grains.Grain;

public interface IBlockGrain : IGrainWithIntegerKey
{
    Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent);

    Task<Dictionary<string, BlockEventData>> GetBlockDictionary();
}

