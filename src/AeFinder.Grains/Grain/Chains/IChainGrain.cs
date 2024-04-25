using AeFinder.Grains.State.Chains;
using Orleans;

namespace AeFinder.Grains.Grain.Chains;

public interface IChainGrain : IGrainWithStringKey
{
    Task SetLatestBlockAsync(string blockHash, long blockHeight);
    Task SetLatestConfirmedBlockAsync(string blockHash, long blockHeight);
    Task<ChainState> GetChainStatusAsync();
}