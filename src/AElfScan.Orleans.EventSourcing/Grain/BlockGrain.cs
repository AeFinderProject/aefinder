using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using AElfScan.EventData;
using AElfScan.State;
using Orleans.EventSourcing;
using Volo.Abp.DependencyInjection;
using AElf.Orleans.EventSourcing.Snapshot;
using Orleans.Providers;
using Volo.Abp.Auditing;
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

    public async Task<Dictionary<string, Block>> GetBlockDictionary()
    {
        return this.State.Blocks;
    }

    public async Task<List<Block>> SaveBlock(BlockEventData blockEvent)
    {
        //Console.WriteLine($"[BlockGrain]Begin Save Block height:{blockEvent.BlockNumber} {blockEvent.BlockHash}");
        //Ignore blocks with height less than LIB block in Dictionary
        var dicLibBlock = this.State.Blocks.Where(b => b.Value.IsConfirmed)
            .Select(x => x.Value)
            .FirstOrDefault();
        if (dicLibBlock != null && dicLibBlock.BlockNumber >= blockEvent.BlockNumber)
        {
            //Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
            return null;
        }
        

        Block currentLibBlock = this.State.FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);
        
        List<Block> libBlockList = new List<Block>();
        if (currentLibBlock != null)
        {
            //Console.WriteLine($"[BlockGrain]currentLibBlock {currentLibBlock.BlockNumber}");
            GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
        }
        
        RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
        await ConfirmEvents();

        //Console.WriteLine($"[BlockGrain]return libBlockList length:{libBlockList.Count}");
        return libBlockList;
    }
    
    private void GetLibBlockList(string currentLibBlockHash, List<Block> libBlockList)
    {
        while (this.State.Blocks.ContainsKey(currentLibBlockHash))
        {
            if (this.State.Blocks[currentLibBlockHash].IsConfirmed)
            {
                libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
                return;
            }
            
            this.State.Blocks[currentLibBlockHash].IsConfirmed = true;
            libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
            currentLibBlockHash = this.State.Blocks[currentLibBlockHash].PreviousBlockHash;
        }
    }
    
}