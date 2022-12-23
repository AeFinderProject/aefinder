using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetInfoGrain : IGrainWithStringKey
{
    Task<long> GetConfirmedBlockHeight(BlockFilterType filterType);

    Task SetConfirmedBlockHeight(BlockFilterType filterType, long blockHeight);
}