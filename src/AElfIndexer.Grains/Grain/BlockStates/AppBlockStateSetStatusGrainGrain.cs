using AElfIndexer.Grains.State.BlockState;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

public class AppBlockStateSetStatusGrainGrain : Grain<AppBlockStateSetStatusState>, IAppBlockStateSetStatusGrain
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