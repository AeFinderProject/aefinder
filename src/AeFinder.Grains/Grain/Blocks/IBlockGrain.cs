using AeFinder.Grains.EventData;
using Orleans;

namespace AeFinder.Grains.Grain.Blocks;

public interface IBlockGrain : IGrainWithStringKey
{
    Task SaveBlock(BlockData block);
    
    Task<BlockData> ConfirmBlock();

    Task DeleteGrainStateAsync();

}

