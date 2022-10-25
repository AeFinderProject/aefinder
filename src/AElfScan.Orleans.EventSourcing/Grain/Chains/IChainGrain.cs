using AElfScan.Orleans.EventSourcing.State.Chains;
using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.Chains;

public interface IChainGrain : IGrainWithStringKey
{
    Task SetLatestBlockAsync(string blockHash, long blockHeight);
    Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight);
    Task<ChainState> GetChainStatusAsync();
}