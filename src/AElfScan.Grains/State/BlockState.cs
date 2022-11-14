using AElfScan.Grains.EventData;

namespace AElfScan.Grains.State;

public class BlockState
{
    public Dictionary<string, BlockEventData> Blocks = new Dictionary<string, BlockEventData>();

    public void Apply(BlockEventData blockEvent)
    {
        //Whether include the LibFound event
        if (blockEvent.LibBlockNumber > 0)
        {
            //Contains LibFound event
            BlockEventData currentLibBlock = FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);

            if (currentLibBlock != null)
            {
                Blocks.RemoveAll(b => b.Value.BlockNumber < blockEvent.LibBlockNumber);
                Blocks.RemoveAll(b =>
                    b.Value.BlockNumber == blockEvent.LibBlockNumber && b.Value.BlockHash != currentLibBlock.BlockHash);
            }
        }
        
        bool addResult = Blocks.TryAdd(blockEvent.BlockHash, blockEvent);
        if (!addResult)
        {
            // TODO: Use Logger
            Console.WriteLine($"[Block State Apply]Add new block{blockEvent.BlockNumber} to dictionary {addResult}");
            Console.WriteLine($"Block hash: {blockEvent.BlockHash} exist: {Blocks.ContainsKey(blockEvent.BlockHash)}");
        }

        Console.WriteLine(
            $"Blocks count: {Blocks.Count}. Lib: {blockEvent.LibBlockNumber}. Block height: {blockEvent.BlockNumber}");
    }

    public BlockEventData FindLibBlock(string previousBlockHash, long libBlockNumber)
    {
        if (libBlockNumber <= 0)
        {
            return null;
        }
        
        while (Blocks.ContainsKey(previousBlockHash))
        {
            if (Blocks[previousBlockHash].BlockNumber == libBlockNumber)
            {
                return Blocks[previousBlockHash];
            }

            previousBlockHash = Blocks[previousBlockHash].PreviousBlockHash;
        }

        return null;
    }

    public void Apply(BlockStateEventData stateEventData)
    {
        Blocks.Add(stateEventData.BlockHash, stateEventData.BlockInfo);
    }
    
}