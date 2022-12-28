using System.Xml;
using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetInfoGrain : Grain<BlockStateSetInfoGrainState>, IBlockStateSetInfoGrain
{
    public Task<long> GetConfirmedBlockHeight(BlockFilterType filterType)
    {
        return State.BlockHeightInfo.TryGetValue(filterType, out var blockHeight)
            ? Task.FromResult(State.BlockHeightInfo[filterType])
            : Task.FromResult(0L);
    }
    
    public async Task SetConfirmedBlockHeight(BlockFilterType filterType, long blockHeight)
    {
        State.BlockHeightInfo[filterType] = blockHeight;
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
}