using AElfScan.EventData;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Grain;

public interface IBlockGrain : Orleans.IGrainWithIntegerKey
{
    Task<int> GetBlockCount();
    Task<bool> NewEvent(BlockEventData @event);
    Task<bool> SaveBlock(BlockEventData blockEvent);
}

