using AeFinder.Grains.State.Chains;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AeFinder.Grains.Grain.Chains;

public class ChainGrain : AeFinderGrain<ChainState>, IChainGrain
{
    private readonly ILogger<ChainGrain> _logger;

    public ChainGrain(ILogger<ChainGrain> logger)
    {
        _logger = logger;

    }

    public async Task SetLatestBlockAsync(string blockHash, long blockHeight)
    {
        await ReadStateAsync();
        if (blockHeight <= State.BlockHeight)
        {
            return;
        }

        State.BlockHash = blockHash;
        State.BlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public async Task SetLatestConfirmedBlockAsync(string blockHash, long blockHeight)
    {
        await ReadStateAsync();
        if (blockHeight <= State.ConfirmedBlockHeight)
        {
            return;
        }
        
        State.ConfirmedBlockHash = blockHash;
        State.ConfirmedBlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public async Task<ChainState> GetChainStatusAsync()
    {
        await ReadStateAsync();
        return State;
    }
}