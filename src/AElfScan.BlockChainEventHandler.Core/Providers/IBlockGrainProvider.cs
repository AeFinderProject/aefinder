using AElfScan.Grains.Grain;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Providers;

public interface IBlockGrainProvider
{
    IBlockGrain GetBlockGrain();
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public IBlockGrain GetBlockGrain()
    {
        return _clusterClient.GetGrain<IBlockGrain>(1);
    }
}