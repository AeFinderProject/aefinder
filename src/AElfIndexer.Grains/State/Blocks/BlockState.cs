using AElfIndexer.Grains.EventData;

namespace AElfIndexer.Grains.State.Blocks;

public class BlockState
{
    public BlockData Block = new BlockData();
    
    // public Dictionary<string, BlockEventData> Blocks = new Dictionary<string, BlockEventData>();
    //
    // public void Apply(BlockEventData blockEvent)
    // {
    //     //Whether include the LibFound event
    //     if (blockEvent.ClearBlockStateDictionary && blockEvent.LibBlockHeight > 0)
    //     {
    //         //Contains LibFound event
    //         BlockEventData currentLibBlock = FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockHeight);
    //     
    //         if (currentLibBlock != null)
    //         {
    //             Blocks.RemoveAll(b => b.Value.BlockHeight < blockEvent.LibBlockHeight);
    //             Blocks.RemoveAll(b =>
    //                 b.Value.BlockHeight == blockEvent.LibBlockHeight && b.Value.BlockHash != currentLibBlock.BlockHash);
    //         }
    //     }
    //     
    //     bool addResult = Blocks.TryAdd(blockEvent.BlockHash, blockEvent);
    //     if (!addResult)
    //     {
    //         // TODO: Use Logger
    //         Console.WriteLine($"[Block State Apply]Add new block{blockEvent.BlockHeight} to dictionary {addResult}");
    //         Console.WriteLine($"Block hash: {blockEvent.BlockHash} exist: {Blocks.ContainsKey(blockEvent.BlockHash)}");
    //     }
    //
    //     // Console.WriteLine(
    //     //     $"Blocks count: {Blocks.Count}. Lib: {blockEvent.LibBlockNumber}. Block height: {blockEvent.BlockNumber}");
    // }
    //
    // public BlockEventData FindLibBlock(string previousBlockHash, long libBlockNumber)
    // {
    //     if (libBlockNumber <= 0)
    //     {
    //         return null;
    //     }
    //     
    //     while (Blocks.ContainsKey(previousBlockHash))
    //     {
    //         if (Blocks[previousBlockHash].BlockHeight == libBlockNumber)
    //         {
    //             return Blocks[previousBlockHash];
    //         }
    //
    //         previousBlockHash = Blocks[previousBlockHash].PreviousBlockHash;
    //     }
    //
    //     return null;
    // }
    //
    // public void Apply(BlockStateEventData stateEventData)
    // {
    //     Blocks.Add(stateEventData.BlockHash, stateEventData.BlockInfo);
    // }
    
}