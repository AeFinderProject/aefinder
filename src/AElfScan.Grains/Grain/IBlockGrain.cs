using AElfScan.Grains.EventData;
using Orleans;

namespace AElfScan.Grains.Grain;

public interface IBlockGrain : IGrainWithStringKey
{
    Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent);

    Task<Dictionary<string, BlockEventData>> GetBlockDictionary();

    Task InitializeStateAsync(Dictionary<string, BlockEventData> blocksDictionary);
}

