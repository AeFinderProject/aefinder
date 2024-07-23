using AeFinder.Grains.EventData;
using AeFinder.Grains.State.Blocks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans;

namespace AeFinder.Grains.Grain.Blocks;

[StorageProvider(ProviderName= "Default")]
public class BlockBranchGrain:AeFinderGrain<BlockBranchState>,IBlockBranchGrain
{
    private readonly ILogger<BlockBranchGrain> _logger;

    public BlockBranchGrain(
        ILogger<BlockBranchGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<Dictionary<string, BlockBasicData>> GetBlockDictionary()
    {
        await ReadStateAsync();
        return State.Blocks;
    }

    public async Task<List<BlockData>> SaveBlocks(List<BlockData> blockEventDataList)
    {
        await ReadStateAsync();
        
        blockEventDataList = await FilterBlockList(blockEventDataList);
        if (blockEventDataList == null) return null;
        
        //save block data by grain
        await SaveBlocksAsync(blockEventDataList);
        
        foreach (var blockEventData in blockEventDataList)
        {
            var basicData = new BlockBasicData()
            {
                ChainId = blockEventData.ChainId,
                BlockHash = blockEventData.BlockHash,
                BlockHeight = blockEventData.BlockHeight,
                PreviousBlockHash = blockEventData.PreviousBlockHash,
                BlockTime = blockEventData.BlockTime,
                Confirmed = blockEventData.Confirmed
            };
            State.Blocks.TryAdd(blockEventData.BlockHash, basicData);
        }

        var libBlockList = await GetLibBlockListAsync(blockEventDataList);
        // await ConfirmBlocksAsync(libBlockList);

        foreach (var libBlockData in libBlockList)
        {
            State.Blocks[libBlockData.BlockHash].Confirmed = true;
        }
        
        var libBlock = libBlockList.OrderBy(x => x.BlockHeight).LastOrDefault();
        if (libBlock != null)
        {
            ClearDictionary(libBlock.BlockHeight, libBlock.BlockHash);
        }

        await WriteStateAsync();
        return libBlockList;
    }

    private async Task SaveBlocksAsync(List<BlockData> blocks)
    {
        var grainTaskList = new List<Task>();
        foreach (var blockItem in blocks)
        {
            var primaryKey = GrainIdHelper.GenerateGrainId(blockItem.ChainId,AeFinderApplicationConsts.BlockGrainIdSuffix,blockItem.BlockHash);
            var blockGrain = GrainFactory.GetGrain<IBlockGrain>(primaryKey);
            grainTaskList.Add(blockGrain.SaveBlock(blockItem));
        }
        await Task.WhenAll(grainTaskList.ToArray());
    }

    // private async Task ConfirmBlocksAsync(List<BlockData> confirmBlocks)
    // {
    //     if (confirmBlocks.Count == 0) return;
    //     var grainTaskList = new List<Task>();
    //     foreach (var blockItem in confirmBlocks)
    //     {
    //         var primaryKey = GrainIdHelper.GenerateGrainId(blockItem.ChainId,
    //             AeFinderApplicationConsts.BlockGrainIdSuffix, blockItem.BlockHash);
    //         var blockGrain = GrainFactory.GetGrain<IBlockGrain>(primaryKey);
    //         grainTaskList.Add(blockGrain.SetBlockConfirmed());
    //     }
    //     await Task.WhenAll(grainTaskList.ToArray());
    // }

    public Task<List<BlockData>> FilterBlockList(List<BlockData> blockEventDataList)
    {
        if (State.Blocks.Count > 0)
        {
            // Ignore blocks with height less than LIB block in Dictionary
            var dicLibBlock = State.Blocks.Where(b => b.Value.Confirmed)
                .Select(x => x.Value)
                .FirstOrDefault();
            if (dicLibBlock != null)
            {
                if (dicLibBlock.BlockHeight >= blockEventDataList.OrderBy(x => x.BlockHeight).Last().BlockHeight)
                {
                    // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
                    return Task.FromResult<List<BlockData>>(null);
                }
            
                blockEventDataList = blockEventDataList.Where(b =>
                        b.BlockHeight > dicLibBlock.BlockHeight)
                    .ToList();
                
                if (blockEventDataList.Count == 0)
                {
                    return Task.FromResult<List<BlockData>>(null);
                }
            }

            //Ensure block continuity
            if (!State.Blocks.ContainsKey(blockEventDataList.First().PreviousBlockHash))
            {
                Console.WriteLine(
                    $"[BlockGrain]Block {blockEventDataList.First().BlockHeight} can't be processed now, its PreviousBlockHash:{blockEventDataList.First().BlockHash} is not exist in dictionary");
                throw new Exception(
                    $"Block {blockEventDataList.First().BlockHeight} can't be processed now, its PreviousBlockHash:{blockEventDataList.First().BlockHash} is not exist in dictionary " +
                    $"which max block height is {State.Blocks.Max(d=>d.Value.BlockHeight)}");
            }

        }

        return Task.FromResult(blockEventDataList);
    }

    private async Task<List<BlockData>> GetLibBlockListAsync(List<BlockData> blockEventDataList)
    {
        long maxLibBlockHeight = blockEventDataList.Max(b => b.LibBlockHeight);
        if (maxLibBlockHeight > 0)
        {
            var blockWithLibEvent = blockEventDataList.First(b => b.LibBlockHeight == maxLibBlockHeight);
            var currentLibBlockHash = FindLibBlock(blockWithLibEvent.PreviousBlockHash,
                blockWithLibEvent.LibBlockHeight);

            if (currentLibBlockHash != null)
            {
                return await GetLibBlockListAsync(currentLibBlockHash);
            }
        }
        return new List<BlockData>();
    }

    private string FindLibBlock(string blockHash, long libBlockHeight)
    {
        if (libBlockHeight <= 0)
        {
            return null;
        }
        
        while (State.Blocks.ContainsKey(blockHash))
        {
            if (State.Blocks[blockHash].BlockHeight == libBlockHeight)
            {
                return blockHash;
            }

            blockHash = State.Blocks[blockHash].PreviousBlockHash;
        }
    
        return null;
    }
    
    private async Task<List<BlockData>> GetLibBlockListAsync(string currentLibBlockHash)
    {
        var libBlockTaskList = new List<Task<BlockData>>();
        while (State.Blocks.ContainsKey(currentLibBlockHash))
        {
            if (State.Blocks[currentLibBlockHash].Confirmed)
            {
                break;
            }
            
            var blockBasicItem = State.Blocks[currentLibBlockHash];
            var primaryKey = GrainIdHelper.GenerateGrainId(blockBasicItem.ChainId,
                AeFinderApplicationConsts.BlockGrainIdSuffix, blockBasicItem.BlockHash);
            var blockGrain = GrainFactory.GetGrain<IBlockGrain>(primaryKey);
            libBlockTaskList.Add(blockGrain.ConfirmBlock());
            currentLibBlockHash = State.Blocks[currentLibBlockHash].PreviousBlockHash;
        }
        return (await Task.WhenAll(libBlockTaskList)).ToList();
    }

    private void ClearDictionary(long libBlockHeight, string libBlockHash)
    {
        State.Blocks.RemoveAll(b => b.Value.BlockHeight < libBlockHeight);
        State.Blocks.RemoveAll(b =>
            b.Value.BlockHeight == libBlockHeight && b.Value.BlockHash != libBlockHash);
    }

    
}