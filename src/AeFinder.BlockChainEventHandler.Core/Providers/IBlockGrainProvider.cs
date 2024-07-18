using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.BlockChainEventHandler.Providers;

public interface IBlockGrainProvider
{
    Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash);
    Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash)
    {
        var primaryKey = GrainIdHelper.GenerateGrainId(chainId,
            AeFinderApplicationConsts.BlockGrainIdSuffix, blockHash);
        var blockGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
        return Task.FromResult(blockGrain);
    }

    public Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId)
    {
        var primaryKey =
            GrainIdHelper.GenerateGrainId(chainId, AeFinderApplicationConsts.BlockBranchGrainIdSuffix);
        var grain = _clusterClient.GetGrain<IBlockBranchGrain>(primaryKey);

        return Task.FromResult(grain);
    }
}