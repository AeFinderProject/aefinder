using AElfIndexer.Grains.EventData;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

public interface IBlockGrain : IGrainWithStringKey
{
    Task SaveBlock(BlockEventData blockEvent);

    Task<BlockEventData> GetBlockEventData();

    Task SetBlockConfirmed();

    Task<bool> IsBlockConfirmed();
    // Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent);
    //
    // Task<List<BlockEventData>> SaveBlocks(List<BlockEventData> blockEventDataList);
    //
    // Task<Dictionary<string, BlockEventData>> GetBlockDictionary();
    //
    // Task InitializeStateAsync(Dictionary<string, BlockEventData> blocksDictionary);
}

