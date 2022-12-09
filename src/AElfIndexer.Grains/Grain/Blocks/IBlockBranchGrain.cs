using AElfIndexer.Grains.EventData;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

public interface IBlockBranchGrain: IGrainWithStringKey
{
    Task<List<BlockEventData>> SaveBlocks(List<BlockEventData> blockEventDataList);
    
    // Task<List<BlockEventData>> CheckBlockList(List<BlockEventData> blockEventDataList);
    // Task<bool> AddBlockToDictionary(BlockEventData blockEventData);
    // Task<List<BlockEventData>> GetLibBlockList(List<BlockEventData> blockEventDataList);
    // Task ClearDictionary(long libBlockHeight,string libBlockHash);
}