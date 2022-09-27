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

        _logger.LogInformation("Start Raise Event of Block Number:" + blockEvent.BlockNumber);
        RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
        await ConfirmEvents();
        Console.WriteLine("Event has been comfirmed! eventtime:" + blockEvent.BlockTime);
        
        return libBlockList;
    }
    
    private Block FindLibBlock(string previousBlockHash, long libBlockNumber)
    {
        if (!this.State.Blocks.ContainsKey(previousBlockHash))
        {
            return null;
        }

        if (this.State.Blocks[previousBlockHash].BlockNumber == libBlockNumber)
        {
            return this.State.Blocks[previousBlockHash];
        }
        else
        {
            return FindLibBlock(this.State.Blocks[previousBlockHash].PreviousBlockHash, libBlockNumber);
        }
    }
    
    private void GetLibBlockList(Block currentLibBlock,List<Block> libBlockList)
    {
        currentLibBlock.IsConfirmed = true;
        libBlockList.Add(currentLibBlock);

        if (!this.State.Blocks.ContainsKey(currentLibBlock.PreviousBlockHash))
        {
            return;
        }

        if (this.State.Blocks[currentLibBlock.PreviousBlockHash].IsConfirmed)
        {
            libBlockList.Add(this.State.Blocks[currentLibBlock.PreviousBlockHash]);
        }
        else
        {
            GetLibBlockList(this.State.Blocks[currentLibBlock.PreviousBlockHash],libBlockList);
        }
    }
    
}