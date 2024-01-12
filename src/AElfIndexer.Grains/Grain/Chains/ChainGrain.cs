using AElfIndexer.Grains.State.Chains;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfIndexer.Grains.Grain.Chains;

public class ChainGrain : Grain<ChainState>, IChainGrain
{
    private readonly ILogger<ChainGrain> _logger;

    public override async Task OnActivateAsync()
    {
        await this.ReadStateAsync();
        await base.OnActivateAsync();
    }

    public ChainGrain(ILogger<ChainGrain> logger)
    {
        _logger = logger;

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