using AeFinder.Grains.EventData;
using Orleans;

namespace AeFinder.Grains.Grain.Blocks;

public interface IBlockBranchGrain: IGrainWithStringKey
{
    Task<List<BlockData>> SaveBlocks(List<BlockData> blockEventDataList);

    Task<Dictionary<string, BlockBasicData>> GetBlockDictionary();

    Task ClearBlockGrainsAsync(List<BlockData> blockDatas);
}