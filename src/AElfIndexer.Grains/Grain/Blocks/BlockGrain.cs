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
    }

    public async Task<BlockData> GetBlockData()
    {
        // if (this.State.Block.Transactions != null)
        // {
        //     //Clear Duplicate transactions
        //     this.State.Block.Transactions = this.State.Block.Transactions.DistinctBy(x=>x.TransactionId).ToList();
        // }
        return State.Block;
    }

    public async Task SetBlockConfirmed()
    {
        State.Block.Confirmed = true;
        await WriteStateAsync();
    }

    public async Task<bool> IsBlockConfirmed()
    {
        return State.Block.Confirmed;
    }

}