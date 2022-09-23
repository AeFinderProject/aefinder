using AElfScan.EventData;

namespace AElfScan.State;

public class BlockState
{
    public Dictionary<string, Block> Blocks = new Dictionary<string, Block>();
    public List<Block> LibBlockList { get; set; }

    public void Apply(BlockEventData blockEvent)
    {
        LibBlockList = new List<Block>();
        
        //Whether include the LibFound event
        if (blockEvent.LibBlockNumber > 0)
        {
            Console.WriteLine("start find lib block");
            //Contains LibFound event
            Block currentLibBlock = FindLibBlock(blockEvent.PreviousBlockHash, blockEvent.LibBlockNumber);
            Console.WriteLine($"find currentLibBlock:{currentLibBlock}");

            if (currentLibBlock != null)
            {
                GetLibBlockList(currentLibBlock);

                Console.WriteLine(LibBlockList.Count);

                List<string> deleteBlockHash = new List<string>();
                foreach (var block in Blocks)
                {
                    //Deal with currentLibBlock's fork block
                    if (block.Value.BlockNumber == currentLibBlock.BlockNumber &&
                        block.Key != currentLibBlock.BlockHash)
                    {
                        deleteBlockHash.Add(block.Key);
                    }

                    if (block.Value.BlockNumber < currentLibBlock.BlockNumber)
                    {
                        deleteBlockHash.Add(block.Key);
                    }

                    //set currentLibBlock in dictionary is confirmed
                    if (block.Value.BlockNumber == currentLibBlock.BlockNumber &&
                        block.Key == currentLibBlock.BlockHash)
                    {
                        block.Value.IsConfirmed = true;
                    }
                }

                foreach (var forkHash in deleteBlockHash)
                {
                    Blocks.Remove(forkHash);
                }
            }
        }
        
        Block newBlock = new Block();
        newBlock.ChainId = blockEvent.ChainId;
        newBlock.BlockHash = blockEvent.BlockHash;
        newBlock.BlockNumber = blockEvent.BlockNumber;
        newBlock.PreviousBlockHash = blockEvent.PreviousBlockHash;
        newBlock.IsConfirmed = false;
        Blocks.Add(blockEvent.BlockHash, newBlock);
    }

    private Block FindLibBlock(string previousBlockHash, long libBlockNumber)
    {
        if (!Blocks.ContainsKey(previousBlockHash))
        {
            return null;
        }

        if (Blocks[previousBlockHash].BlockNumber == libBlockNumber)
        {
            return Blocks[previousBlockHash];
        }
        else
        {
            FindLibBlock(Blocks[previousBlockHash].PreviousBlockHash, libBlockNumber);
        }

        return null;
    }

    private void GetLibBlockList(Block currentLibBlock)
    {
        currentLibBlock.IsConfirmed = true;
        LibBlockList.Add(currentLibBlock);

        if (!Blocks.ContainsKey(currentLibBlock.PreviousBlockHash))
        {
            return;
        }

        if (Blocks[currentLibBlock.PreviousBlockHash].IsConfirmed)
        {
            LibBlockList.Add(Blocks[currentLibBlock.PreviousBlockHash]);
        }
        else
        {
            GetLibBlockList(Blocks[currentLibBlock.PreviousBlockHash]);
        }
    }
}