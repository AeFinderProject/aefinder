using AElfScan.EventData;
using AElfScan.State;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Grain;

public interface IBlockGrain : IGrainWithIntegerKey
{
    Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent);

    Task<Dictionary<string, BlockEventData>> GetBlockDictionary();
}

