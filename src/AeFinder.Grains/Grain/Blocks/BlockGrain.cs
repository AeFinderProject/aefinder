using AeFinder.Grains.EventData;
using AeFinder.Grains.State.Blocks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans;

namespace AeFinder.Grains.Grain.Blocks;

// public class BlockGrain:JournaledSnapshotGrain<BlockState>,IBlockGrain
[StorageProvider(ProviderName= "Default")]
public class BlockGrain:AeFinderGrain<BlockState>,IBlockGrain
{
    private readonly ILogger<BlockGrain> _logger;
    
    public BlockGrain(
        ILogger<BlockGrain> logger)
    {
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (this.State != null && this.State.Block.BlockHeight > 0)
        {
            await WriteStateAsync();
        }
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task<BlockData> GetBlock()
    {
        await ReadStateAsync();
        return State.Block;
    }

    public async Task SaveBlock(BlockData block)
    {
        await ReadStateAsync();
        State.Block = block;
        await WriteStateAsync();
        _logger.LogInformation($"save block {State.Block.BlockHeight} success");
        // DeactivateOnIdle();
        // DelayDeactivation(TimeSpan.FromMinutes(1));
    }

    public async Task<BlockData> ConfirmBlock()
    {
        await ReadStateAsync();
        State.Block.Confirmed = true;
        foreach (var transaction in State.Block.Transactions)
        {
            transaction.Confirmed = true;
            foreach (var logEvent in transaction.LogEvents)
            {
                logEvent.Confirmed = true;
            }
        }
        _logger.LogInformation($"before write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");
        await WriteStateAsync();
        _logger.LogInformation($"after write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");

        return State.Block;
        // DelayDeactivation(TimeSpan.FromSeconds(5));
    }
    
    public async Task DeleteGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}