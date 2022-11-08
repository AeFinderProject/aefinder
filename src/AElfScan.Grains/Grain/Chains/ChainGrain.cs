using AElfScan.Grains.State.Chains;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfScan.Grains.Grain.Chains;

public class ChainGrain : Grain<ChainState>, IChainGrain
{
    private readonly ILogger logger;

    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public ChainGrain(ILogger<ChainGrain> logger)
    {
        this.logger = logger;

    }

    public async Task SetLatestBlockAsync(string blockHash, long blockHeight)
    {
        if (blockHeight <= State.BlockHeight)
        {
            return;
        }

        State.BlockHash = blockHash;
        State.BlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public async Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight)
    {
        if (blockHeight <= State.ConfirmedBlockHeight)
        {
            return;
        }
        
        State.ConfirmedBlockHash = blockHash;
        State.ConfirmedBlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public Task<ChainState> GetChainStatusAsync()
    {
        return Task.FromResult(State);
    }
}