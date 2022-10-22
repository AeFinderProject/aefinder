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

    public async Task<Dictionary<string, BlockEventData>> GetBlockDictionary()
    {
        return this.State.Blocks;
    }

    public async Task<List<BlockEventData>> SaveBlock(BlockEventData blockEvent)
    {
        //Ignore blocks with height less than LIB block in Dictionary
        var dicLibBlock = this.State.Blocks.Where(b => b.Value.IsConfirmed)
            .Select(x => x.Value)
            .FirstOrDefault();
        if (dicLibBlock != null)
        {
            if (dicLibBlock.BlockNumber >= blockEvent.BlockNumber)
            {
                // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
                return null;
            }

            if (!this.State.Blocks.ContainsKey(blockEvent.PreviousBlockHash))
            {
                Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
                throw new Exception($"Block {blockEvent.BlockNumber} can't be processed now, its PreviousBlockHash is not exist in dictionary");
            }
        }
        

        BlockEventData currentLibBlock = this.State.FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);
        
        List<BlockEventData> libBlockList = new List<BlockEventData>();
        if (currentLibBlock != null)
        {
            GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
        }
        
        RaiseEvent(blockEvent, blockEvent.LibBlockNumber > 0);
        await ConfirmEvents();

        return libBlockList;
    }
    
    private void GetLibBlockList(string currentLibBlockHash, List<BlockEventData> libBlockList)
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