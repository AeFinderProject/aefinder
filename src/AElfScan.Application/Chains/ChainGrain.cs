using System.Threading.Tasks;
using AElfScan.Orleans;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfScan.Chains;

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

    public ChainGrain(ILogger<HelloGrain> logger)
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