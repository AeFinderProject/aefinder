using AElfIndexer.Grains.EventData;
using AElfIndexer.Grains.State.Blocks;
using Orleans.Providers;
using Orleans;
using Volo.Abp.Threading;

namespace AElfIndexer.Grains.Grain.Blocks;

[StorageProvider(ProviderName= "Default")]
public class BlockDictionaryGrain:Grain<BlockDictionaryState>,IBlockDictionaryGrain
{

    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }

    public async Task<List<BlockEventData>> CheckBlockList(List<BlockEventData> blockEventDataList)
    {
        if (this.State.Blocks.Count > 0)
        {
            // Ignore blocks with height less than LIB block in Dictionary
            var dicLibBlock = this.State.Blocks.Where(b => b.Value.Confirmed)
                .Select(x => x.Value)
                .FirstOrDefault();
            if (dicLibBlock != null)
            {
                if (dicLibBlock.BlockHeight >= blockEventDataList.OrderBy(x => x.BlockHeight).Last().BlockHeight)
                {
                    // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
                    return null;
                }

                blockEventDataList = blockEventDataList.Where(b =>
                        b.BlockHeight > dicLibBlock.BlockHeight &&
                        !State.Blocks.ContainsKey(b.BlockHash))
                    .ToList();
                
                if (blockEventDataList.Count == 0)
                {
                    return null;
                }
            }

            //Ensure block continuity
            if (!this.State.Blocks.ContainsKey(blockEventDataList.First().PreviousBlockHash))
            {
                Console.WriteLine(
                    $"[BlockGrain]Block {blockEventDataList.First().BlockHeight} can't be processed now, its PreviousBlockHash is not exist in dictionary");
                throw new Exception(
                    $"Block {blockEventDataList.First().BlockHeight} can't be processed now, its PreviousBlockHash is not exist in dictionary");
            }

        }

        return blockEventDataList;
    }
    
    public async Task<bool> AddBlockToDictionary(BlockEventData blockEventData)
    {
        return this.State.Blocks.TryAdd(blockEventData.BlockHash, blockEventData);
    }

    public async Task<List<BlockEventData>> GetLibBlockList(List<BlockEventData> blockEventDataList)
    {
        List<BlockEventData> libBlockList = new List<BlockEventData>();


        long maxLibBlockHeight = blockEventDataList.Max(b => b.LibBlockHeight);

        if (maxLibBlockHeight > 0)
        {
            var index = blockEventDataList.FindLastIndex(b => b.LibBlockHeight == maxLibBlockHeight);
            var blockWithLibEvent = blockEventDataList[index];
            var currentLibBlock = FindLibBlock(blockWithLibEvent.PreviousBlockHash,
                blockWithLibEvent.LibBlockHeight);

            if (currentLibBlock != null)
            {
                GetLibBlockList(currentLibBlock.BlockHash, libBlockList);
            }

            // blockEvent.ClearBlockStateDictionary = true;
        }
        
        return libBlockList;

    }

    public BlockEventData FindLibBlock(string previousBlockHash, long libBlockHeight)
    {
        if (libBlockHeight <= 0)
        {
            return null;
        }
        
        while (this.State.Blocks.ContainsKey(previousBlockHash))
        {
            if (this.State.Blocks[previousBlockHash].BlockHeight == libBlockHeight)
            {
                return this.State.Blocks[previousBlockHash];
            }
    
            previousBlockHash = this.State.Blocks[previousBlockHash].PreviousBlockHash;
        }
    
        return null;
    }
    
    private void GetLibBlockList(string currentLibBlockHash, List<BlockEventData> libBlockList)
    {
        while (this.State.Blocks.ContainsKey(currentLibBlockHash))
        {
            if (this.State.Blocks[currentLibBlockHash].Confirmed)
            {
                libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
                return;
            }
            
            this.State.Blocks[currentLibBlockHash].Confirmed = true;
            libBlockList.Add(this.State.Blocks[currentLibBlockHash]);
            currentLibBlockHash = this.State.Blocks[currentLibBlockHash].PreviousBlockHash;
        }
    }

    public async Task ClearDictionary(long libBlockHeight, string libBlockHash)
    {
        this.State.Blocks.RemoveAll(b => b.Value.BlockHeight < libBlockHeight);
        this.State.Blocks.RemoveAll(b =>
            b.Value.BlockHeight == libBlockHeight && b.Value.BlockHash != libBlockHash);

        AsyncHelper.RunSync(async () =>
        {
            await this.WriteStateAsync();
        });
        
    }
}