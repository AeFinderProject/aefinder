using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

public interface IAppBlockStateSetGrain : IGrainWithStringKey
{
    Task<AppBlockStateSet> GetBlockStateSetAsync();
    Task SetBlockStateSetAsync(AppBlockStateSet set);
    Task RemoveBlockStateSetAsync();
}