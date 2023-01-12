using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Grains.EventData;
using AElfIndexer.Grains.State.Blocks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans;
using Volo.Abp.Threading;

namespace AElfIndexer.Grains.Grain.Blocks;

[StorageProvider(ProviderName= "Default")]
public class BlockBranchGrain:Grain<BlockBranchState>,IBlockBranchGrain
{
    private readonly ILogger<BlockBranchGrain> _logger;

    public BlockBranchGrain(
        ILogger<BlockBranchGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<Dictionary<string, BlockData>> GetBlockDictionary()
    {
        return this.State.Blocks;
    }

    public async Task<List<BlockData>> SaveBlocks(List<BlockData> blockEventDataList)
    {
        blockEventDataList = await FilterBlockList(blockEventDataList);
        if (blockEventDataList == null) return null;
        
        //save block data by grain
        await SaveBlocksAsync(blockEventDataList);
        
        foreach (var blockEventData in blockEventDataList)
        {
            State.Blocks.TryAdd(blockEventData.BlockHash, blockEventData);
        }

        var libBlockList = GetLibBlockList(blockEventDataList);
        await ConfirmBlocksAsync(libBlockList);
        
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
            var primaryKey = GrainIdHelper.GenerateGrainId(blockItem.ChainId,AElfIndexerApplicationConsts.BlockGrainIdSuffix,blockItem.BlockHash);
            var blockGrain = GrainFactory.GetGrain<IBlockGrain>(primaryKey);
            grainTaskList.Add(blockGrain.SaveBlock(blockItem));
        }
        await Task.WhenAll(grainTaskList.ToArray());
    }

    private async Task ConfirmBlocksAsync(List<BlockData> confirmBlocks)
    {
        if (confirmBlocks.Count == 0) return;
        var grainTaskList = new List<Task>();
        foreach (var blockItem in confirmBlocks)
        {
            var primaryKey = GrainIdHelper.GenerateGrainId(blockItem.ChainId,
                AElfIndexerApplicationConsts.BlockGrainIdSuffix, blockItem.BlockHash);
            var blockGrain = GrainFactory.GetGrain<IBlockGrain>(primaryKey);
            grainTaskList.Add(blockGrain.SetBlockConfirmed());
        }
        await Task.WhenAll(grainTaskList.ToArray());
    }

    public async Task<List<BlockData>> FilterBlockList(List<BlockData> blockEventDataList)
    {
        if (State.Blocks.Count > 0)
        {
            // Ignore blocks with height less than LIB block in Dictionary
            // var dicLibBlock = State.Blocks.Where(b => b.Value.Confirmed)
            //     .Select(x => x.Value)
            //     .FirstOrDefault();
            // if (dicLibBlock != null)
            // {
            //     if (dicLibBlock.BlockHeight >= blockEventDataList.OrderBy(x => x.BlockHeight).Last().BlockHeight)
            //     {
            //         // Console.WriteLine($"[BlockGrain]Block {blockEvent.BlockNumber} smaller than dicLibBlock {dicLibBlock.BlockNumber},so ignored");
            //         return null;
            //     }
            //
            //     blockEventDataList = blockEventDataList.Where(b =>
            //             b.BlockHeight > dicLibBlock.BlockHeight &&
            //             !State.Blocks.ContainsKey(b.BlockHash))
            //         .ToList();
            //     
            //     if (blockEventDataList.Count == 0)
            //     {
            //         return null;
            //     }
            // }

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

        return blockEventDataList;
    }

    private List<BlockData> GetLibBlockList(List<BlockData> blockEventDataList)
    {
        long maxLibBlockHeight = blockEventDataList.Max(b => b.LibBlockHeight);
        if (maxLibBlockHeight > 0)
        {
            var blockWithLibEvent = blockEventDataList.First(b => b.LibBlockHeight == maxLibBlockHeight);
            var currentLibBlock = FindLibBlock(blockWithLibEvent.PreviousBlockHash,
                blockWithLibEvent.LibBlockHeight);

            if (currentLibBlock != null)
            {
                return GetLibBlockList(currentLibBlock.BlockHash);
            }
        }
        return new List<BlockData>();
    }

    private BlockData FindLibBlock(string blockHash, long libBlockHeight)
    {
        if (libBlockHeight <= 0)
        {
            return null;
        }
        
        while (State.Blocks.ContainsKey(blockHash))
        {
            if (State.Blocks[blockHash].BlockHeight == libBlockHeight)
            {
                return State.Blocks[blockHash];
            }
    
            blockHash = State.Blocks[blockHash].PreviousBlockHash;
        }
    
        return null;
    }
    
    private List<BlockData> GetLibBlockList(string currentLibBlockHash)
    {
        var libBlockList = new List<BlockData>();
        while (State.Blocks.ContainsKey(currentLibBlockHash))
        {
            if (State.Blocks[currentLibBlockHash].Confirmed)
            {
                // libBlockList.Add(State.Blocks[currentLibBlockHash]);
                return libBlockList;
            }
            
            State.Blocks[currentLibBlockHash].Confirmed = true;
            libBlockList.Add(State.Blocks[currentLibBlockHash]);
            currentLibBlockHash = State.Blocks[currentLibBlockHash].PreviousBlockHash;
        }

        return libBlockList;
    }

    private void ClearDictionary(long libBlockHeight, string libBlockHash)
    {
        State.Blocks.RemoveAll(b => b.Value.BlockHeight < libBlockHeight);
        State.Blocks.RemoveAll(b =>
            b.Value.BlockHeight == libBlockHeight && b.Value.BlockHash != libBlockHash);
    }
}