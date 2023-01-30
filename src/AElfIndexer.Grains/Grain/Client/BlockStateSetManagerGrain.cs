using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Options;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetManagerGrain<T>: Grain<BlockStateSetManagerState<T>>, IBlockStateSetManagerGrain<T>
{
    private readonly ClientOptions _clientOptions;

    public BlockStateSetManagerGrain(IOptionsSnapshot<ClientOptions> clientOptions)
    {
        _clientOptions = clientOptions.Value;
    }

    public async Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets)
    {
        var maxIndex = Math.Max((sets.Count - 1) / _clientOptions.MaxCountPerBlockStateSetBucket + 1,
            State.BlockStateSets.Count);

        for (var i = 0; i < maxIndex; i++)
        {
            var key = GrainIdHelper.GenerateGrainId(this.GetPrimaryKeyString(), i.ToString());
            var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
            var blockSets = sets.Select(o => o).Skip(_clientOptions.MaxCountPerBlockStateSetBucket * i)
                .Take(_clientOptions.MaxCountPerBlockStateSetBucket).ToDictionary(o => o.Key, o => o.Value);
            await grain.SetBlockStateSetsAsync(blockSets);
            State.BlockStateSets[key] = blockSets.Keys.ToHashSet();
        }

        await WriteStateAsync();
    }

    public async Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync()
    {
        var result = new Dictionary<string, BlockStateSet<T>>();
        foreach (var (key, sets) in State.BlockStateSets)
        {
            var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
            var blockStateSets = await grain.GetBlockStateSetsAsync();
            foreach (var set in blockStateSets)
            {
                result[set.Key] = set.Value;
            }
        }

        return result;
    }

    public async Task SetLongestChainBlockHashAsync(string blockHash)
    {
        foreach (var (key, sets) in State.BlockStateSets)
        {
            if (!sets.Contains(blockHash))
            {
                continue;
            }

            State.LongestChainBlockHash = blockHash;
            await WriteStateAsync();
            return;
        }
    }
    
    public async Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync()
    {
        if (string.IsNullOrWhiteSpace(State.LongestChainBlockHash))
        {
            return null;
        }
        
        foreach (var (key,sets) in State.BlockStateSets)
        {
            if (sets.Contains(State.LongestChainBlockHash))
            {
                var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
                return await grain.GetBlockStateSetAsync(State.LongestChainBlockHash);
            }
        }

        return null;
    }
    
    public async Task SetBestChainBlockHashAsync(string blockHash)
    {
        foreach (var (key, sets) in State.BlockStateSets)
        {
            if (!sets.Contains(blockHash))
            {
                continue;
            }

            State.BestChainBlockHash = blockHash;
            await WriteStateAsync();
            return;
        }
    }
    
    public async Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync()
    {
        if (string.IsNullOrWhiteSpace(State.BestChainBlockHash))
        {
            return null;
        }

        foreach (var (key,sets) in State.BlockStateSets)
        {
            if (sets.Contains(State.BestChainBlockHash))
            {
                var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
                return await grain.GetBlockStateSetAsync(State.BestChainBlockHash);
            }
        }

        return null;
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
}