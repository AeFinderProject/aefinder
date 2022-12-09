using AElfIndexer.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public interface IBlockGrainProvider
{
    // Task<IBlockGrain> GetBlockGrain(string chainId);

    // Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash);

    Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    // public async Task<IBlockGrain> GetBlockGrain(string chainId)
    // {
    //     var primaryKeyGrain = _clusterClient.GetGrain<IPrimaryKeyGrain>(chainId + AElfIndexerConsts.PrimaryKeyGrainIdSuffix);
    //     var currentPrimaryKey = await primaryKeyGrain.GetCurrentGrainPrimaryKey(chainId);
    //     var primaryKey = await primaryKeyGrain.GetGrainPrimaryKey(chainId);
    //     
    //     if (currentPrimaryKey == primaryKey)
    //     {
    //         return _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
    //     }
    //
    //     var oldGrain = _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
    //     var blocksDictionary =  await oldGrain.GetBlockDictionary();
    //     
    //     var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
    //     await newGrain.InitializeStateAsync(blocksDictionary);
    //     
    //     return newGrain;
    // }

    // public async Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash)
    // {
    //     string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
    //     var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
    //
    //     return newGrain;
    // }

    public async Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockDictionaryGrainIdSuffix;
        var grain = _clusterClient.GetGrain<IBlockBranchGrain>(primaryKey);

        return grain;
    }
}