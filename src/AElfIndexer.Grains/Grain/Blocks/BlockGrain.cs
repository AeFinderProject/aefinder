using System;
using System.Threading.Tasks;
using AElfIndexer.Grains.EventData;
using AElfIndexer.Grains.State.Blocks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans;

namespace AElfIndexer.Grains.Grain.Blocks;

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
    
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public async Task SaveBlock(BlockData block)
    {
        State.Block = block;
        await WriteStateAsync();
        _logger.LogInformation($"save block {State.Block.BlockHeight} success");
        // DeactivateOnIdle();
        // DelayDeactivation(TimeSpan.FromMinutes(1));
    }

    public async Task<BlockData> GetBlockData()
    {
        return State.Block;
    }

    public async Task SetBlockConfirmed()
    {
        State.Block.Confirmed = true;
        _logger.LogInformation($"before write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");
        await WriteStateAsync();
        _logger.LogInformation($"after write block {State.Block.BlockHeight} confirmed {State.Block.Confirmed} success");
        DeactivateOnIdle();
        // DelayDeactivation(TimeSpan.FromSeconds(5));
    }

    public async Task<bool> IsBlockConfirmed()
    {
        return State.Block.Confirmed;
    }

}