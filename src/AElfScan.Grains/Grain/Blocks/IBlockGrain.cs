using AElfScan.Grains.EventData;
using Orleans;

namespace AElfScan.Grains.Grain.Blocks;

public interface IBlockGrain : IGrainWithStringKey
{
    Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent);

    Task<List<BlockEventData>> SaveBlocks(List<BlockEventData> blockEventDataList);

    Task<Dictionary<string, BlockEventData>> GetBlockDictionary();

    Task InitializeStateAsync(Dictionary<string, BlockEventData> blocksDictionary);
}

