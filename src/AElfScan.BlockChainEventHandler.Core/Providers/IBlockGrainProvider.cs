using AElfScan.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Providers;

public interface IBlockGrainProvider
{
    Task<IBlockGrain> GetBlockGrain(string chainId);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<IBlockGrain> GetBlockGrain(string chainId)
    {
        var primaryKeyGrain = _clusterClient.GetGrain<IPrimaryKeyGrain>(chainId + "BlockGrainPrimaryKey");
        var currentPrimaryKey = await primaryKeyGrain.GetCurrentGrainPrimaryKey(chainId);
        var primaryKey = await primaryKeyGrain.GetGrainPrimaryKey(chainId);
        
        if (currentPrimaryKey == primaryKey)
        {
            return _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
        }

        var oldGrain = _clusterClient.GetGrain<IBlockGrain>(currentPrimaryKey);
        var blocksDictionary =  await oldGrain.GetBlockDictionary();
        
        var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
        await newGrain.InitializeStateAsync(blocksDictionary);
        
        return newGrain;
    }
}