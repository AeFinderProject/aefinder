using System.Threading.Tasks;
using AElfIndexer.Grains.EventData;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

public interface IBlockGrain : IGrainWithStringKey
{
    Task SaveBlock(BlockData block);
    
    Task SetBlockConfirmed();
    
}

