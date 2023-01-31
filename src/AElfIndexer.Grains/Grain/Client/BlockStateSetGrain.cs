using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetGrain<T>: Grain<BlockStateSetState>, IBlockStateSetGrain<T>
{
    private readonly ClientOptions _clientOptions;

    public BlockStateSetGrain(IOptionsSnapshot<ClientOptions> clientOptions)
    {
        _clientOptions = clientOptions.Value;
    }

    public async Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets)
    {
        State.BlockStateSetVersion = GenerateNewBlockStateSetVersion();
        
        var maxIndex = Math.Max((sets.Count - 1) / _clientOptions.MaxCountPerBlockStateSetBucket + 1,
            State.BlockStateSets.Count);

        for (var i = 0; i < maxIndex; i++)
        {
            var key = GrainIdHelper.GenerateGrainId(this.GetPrimaryKeyString(), i.ToString());
            var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
            var blockSets = sets.Select(o => o).Skip(_clientOptions.MaxCountPerBlockStateSetBucket * i)
                .Take(_clientOptions.MaxCountPerBlockStateSetBucket).ToDictionary(o => o.Key, o => o.Value);
            await grain.SetBlockStateSetsAsync(State.BlockStateSetVersion, blockSets);
            State.BlockStateSets[key] = blockSets.Keys.ToHashSet();
        }
        
        await WriteStateAsync();
        
        await CleanInvalidBlockStateSetAsync();
    }

    public async Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync()
    {
        await CleanInvalidBlockStateSetAsync();
        
        var result = new Dictionary<string, BlockStateSet<T>>();
        foreach (var (key, sets) in State.BlockStateSets)
        {
            var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
            var blockStateSets = await grain.GetBlockStateSetsAsync(State.BlockStateSetVersion);
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
                return await grain.GetBlockStateSetAsync(State.BlockStateSetVersion, State.LongestChainBlockHash);
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
                return await grain.GetBlockStateSetAsync(State.BlockStateSetVersion, State.BestChainBlockHash);
            }
        }

        return null;
    }

    private async Task CleanInvalidBlockStateSetAsync()
    {
        var tasks = State.BlockStateSets.Keys.Select(async key =>
        {
            var grain = GrainFactory.GetGrain<IBlockStateSetBucketGrain<T>>(key);
            await grain.CleanAsync(State.BlockStateSetVersion);
        });

        await tasks.WhenAll();
    }

    private string GenerateNewBlockStateSetVersion()
    {
        return Guid.NewGuid().ToString("N");
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();

        if (string.IsNullOrEmpty(State.BlockStateSetVersion))
        {
            State.BlockStateSetVersion = GenerateNewBlockStateSetVersion();
            await WriteStateAsync();
        }

        await base.OnActivateAsync();
    }
}