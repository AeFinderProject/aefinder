using AElfIndexer.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public interface IBlockDictionaryGrainProvider
{
    Task<IBlockDictionaryGrain> GetBlockDictionaryGrain(string chainId);
}

public class BlockDictionaryGrainProvider: IBlockDictionaryGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockDictionaryGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public async Task<IBlockDictionaryGrain> GetBlockDictionaryGrain(string chainId)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockDictionaryGrainIdSuffix;
        var grain = _clusterClient.GetGrain<IBlockDictionaryGrain>(primaryKey);

        return grain;
    }
}