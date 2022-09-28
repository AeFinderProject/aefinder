using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using AElfScan.EventData;
using AElfScan.State;
using Orleans.EventSourcing;
using Volo.Abp.DependencyInjection;
using Orleans.EventSourcing.Snapshot;
using Orleans.Providers;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.Grain;

public class BlockGrain:JournaledSnapshotGrain<BlockState>,IBlockGrain
{
    private readonly ILogger<BlockGrain> _logger;
    
    public BlockGrain(
        ILogger<BlockGrain> logger)
    {
        _logger = logger;
    }

    public async Task<List<Block>> SaveBlock(BlockEventData blockEvent)
    {
        //Ignore blocks with height less than LIB block in Dictionary
        foreach (var block in this.State.Blocks)
        {
            if (block.Value.IsConfirmed && blockEvent.BlockNumber <= block.Value.BlockNumber)
            {
                return null;
            }
        }

        Block currentLibBlock = FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);
        
        List<Block> libBlockList = new List<Block>();
        if (currentLibBlock != null)
        {
            GetLibBlockList(currentLibBlock, libBlockList);
        }
        
        RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
        await ConfirmEvents();

        return libBlockList;
    }
    
    private Block FindLibBlock(string previousBlockHash, long libBlockNumber)
    {
        while (this.State.Blocks.ContainsKey(previousBlockHash))
        {
            if (this.State.Blocks[previousBlockHash].BlockNumber == libBlockNumber)
            {
                return this.State.Blocks[previousBlockHash];
            }

            previousBlockHash = this.State.Blocks[previousBlockHash].PreviousBlockHash;
        }

        return null;
    }
    
    private void GetLibBlockList(Block currentLibBlock,List<Block> libBlockList)
    {
        currentLibBlock.IsConfirmed = true;
        libBlockList.Add(currentLibBlock);

        while (this.State.Blocks.ContainsKey(currentLibBlock.PreviousBlockHash))
        {
            if (this.State.Blocks[currentLibBlock.PreviousBlockHash].IsConfirmed)
            {
                libBlockList.Add(this.State.Blocks[currentLibBlock.PreviousBlockHash]);
                return;
            }
            
            this.State.Blocks[currentLibBlock.PreviousBlockHash].IsConfirmed = true;
            libBlockList.Add(this.State.Blocks[currentLibBlock.PreviousBlockHash]);
            currentLibBlock = this.State.Blocks[currentLibBlock.PreviousBlockHash];
        }
    }
    
}