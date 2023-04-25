using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Grains.EventData;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

public interface IBlockBranchGrain: IGrainWithStringKey
{
    Task<List<BlockData>> SaveBlocks(List<BlockData> blockEventDataList);

    Task<Dictionary<string, BlockBasicData>> GetBlockDictionary();
}