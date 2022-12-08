using AElfIndexer.Grains.EventData;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

public interface IBlockGrain : IGrainWithStringKey
{
    Task SaveBlock(BlockEventData blockEvent);

    Task<long> GetBlockHeight();

    Task<BlockEventData> GetBlockEventData();

    Task<string> GetBlockPreviousBlockHash();

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

