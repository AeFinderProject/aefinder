using AeFinder.Grains.State.BlockStates;
using Orleans;
using Volo.Abp;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppBlockStateSetStatusGrain : AeFinderGrain<AppBlockStateSetStatusState>, IAppBlockStateSetStatusGrain
{
    public async Task<BlockStateSetStatus> GetBlockStateSetStatusAsync()
    {
        await ReadStateAsync();
        return new BlockStateSetStatus
        {
            BestChainBlockHash = State.BestChainBlockHash,
            BestChainHeight = State.BestChainHeight,
            LongestChainBlockHash = State.LongestChainBlockHash,
            LongestChainHeight = State.LongestChainHeight,
            LastIrreversibleBlockHash = State.LastIrreversibleBlockHash,
            LastIrreversibleBlockHeight = State.LastIrreversibleBlockHeight,
            Branches = State.Branches,
        };
    }

    public async Task SetBlockStateSetStatusAsync(BlockStateSetStatus status)
    {
        await ReadStateAsync();

        if (State.LastIrreversibleBlockHeight > 0 &&
            State.LastIrreversibleBlockHeight > status.LastIrreversibleBlockHeight)
        {
            throw new ApplicationException(
                $"Cannot set status, new lib {status.LastIrreversibleBlockHeight} less then current lib {State.LastIrreversibleBlockHeight}.");
        }

        await BeginChangingStateAsync();
        
        State.LongestChainBlockHash = status.LongestChainBlockHash;
        State.LongestChainHeight = status.LongestChainHeight;
        State.BestChainBlockHash = status.BestChainBlockHash;
        State.BestChainHeight = status.BestChainHeight;
        State.LastIrreversibleBlockHash = status.LastIrreversibleBlockHash;
        State.LastIrreversibleBlockHeight = status.LastIrreversibleBlockHeight;
        State.Branches = status.Branches;
        await WriteStateAsync();
    }
}