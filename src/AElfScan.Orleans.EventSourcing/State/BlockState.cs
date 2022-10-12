using AElfScan.EventData;

namespace AElfScan.State;

public class BlockState
{
    public Dictionary<string, Block> Blocks = new Dictionary<string, Block>();

    public void Apply(BlockEventData blockEvent)
    {
        //Whether include the LibFound event
        if (blockEvent.LibBlockNumber > 0)
        {
            //Contains LibFound event
            Block currentLibBlock = FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);

            if (currentLibBlock != null)
            {
                Blocks.RemoveAll(b => b.Value.BlockNumber < blockEvent.LibBlockNumber);
                Blocks.RemoveAll(b =>
                    b.Value.BlockNumber == blockEvent.LibBlockNumber && b.Value.BlockHash != currentLibBlock.BlockHash);
            }
        }
        
        Block newBlock = new Block();
        newBlock.ChainId = blockEvent.ChainId;
        newBlock.BlockHash = blockEvent.BlockHash;
        newBlock.BlockNumber = blockEvent.BlockNumber;
        newBlock.PreviousBlockHash = blockEvent.PreviousBlockHash;
        newBlock.IsConfirmed = false;
        Blocks.TryAdd(blockEvent.BlockHash, newBlock);
    }

    public Block FindLibBlock(string previousBlockHash, long libBlockNumber)
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
    
}