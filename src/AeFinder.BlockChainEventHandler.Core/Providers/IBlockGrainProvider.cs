using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.BlockChainEventHandler.Providers;

public interface IBlockGrainProvider
{
    Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId)
    {
        var primaryKey =
            GrainIdHelper.GenerateGrainId(chainId, AeFinderApplicationConsts.BlockBranchGrainIdSuffix);
        var grain = _clusterClient.GetGrain<IBlockBranchGrain>(primaryKey);

        return grain;
    }
}