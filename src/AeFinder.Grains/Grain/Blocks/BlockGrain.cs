using AeFinder.Grains.EventData;
using AeFinder.Grains.State.Blocks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans;

namespace AeFinder.Grains.Grain.Blocks;

// public class BlockGrain:JournaledSnapshotGrain<BlockState>,IBlockGrain
[StorageProvider(ProviderName= "Default")]
public class BlockGrain:Grain<BlockState>,IBlockGrain
{
    private readonly ILogger<BlockGrain> _logger;
    
    public BlockGrain(
        ILogger<BlockGrain> logger)
    {
        _logger = logger;
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
    
    public Task<BlockData> GetBlock()
    {
        return Task.FromResult(State.Block);
    }

    public async Task SaveBlock(BlockData block)
    {
        State.Block = block;
        await WriteStateAsync();
        _logger.LogInformation($"save block {State.Block.BlockHeight} success");
        // DeactivateOnIdle();
        // DelayDeactivation(TimeSpan.FromMinutes(1));
    }

    public async Task<BlockData> ConfirmBlock()
    {
        State.Block.Confirmed = true;
        _logger.LogInformation($"before write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");
        await WriteStateAsync();
        _logger.LogInformation($"after write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");
        DeactivateOnIdle();

        return State.Block;
        // DelayDeactivation(TimeSpan.FromSeconds(5));
    }
}