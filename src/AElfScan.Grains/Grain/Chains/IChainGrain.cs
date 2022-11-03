using AElfScan.Grains.State.Chains;
using Orleans;

namespace AElfScan.Grains.Grain.Chains;

public interface IChainGrain : IGrainWithStringKey
{
    Task SetLatestBlockAsync(string blockHash, long blockHeight);
    Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight);
    Task<ChainState> GetChainStatusAsync();
}