using AElfScan.Grain.Contracts.Chains;
using Microsoft.Extensions.Logging;

namespace AElfScan.Grain.Chains;

public class ChainGrain : Orleans.Grain<ChainState>, IChainGrain
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
        State.BlockHash = blockHash;
        State.BlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public async Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight)
    {
        State.ConfirmBlockHash = blockHash;
        State.ConfirmBlockHeight = blockHeight;
        await WriteStateAsync();
    }

    public Task<ChainStatusDto> GetChainStatusAsync()
    {
        return Task.FromResult(new ChainStatusDto
        {
            BlockHash = State.BlockHash,
            BlockHeight = State.BlockHeight,
            ConfirmBlockHash = State.ConfirmBlockHash,
            ConfirmBlockHeight = State.ConfirmBlockHeight,
        });
    }
}