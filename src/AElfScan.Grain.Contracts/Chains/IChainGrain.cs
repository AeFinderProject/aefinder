using Orleans;

namespace AElfScan.Grain.Contracts.Chains;

public interface IChainGrain : IGrainWithStringKey
{
    Task SetLatestBlockAsync(string blockHash, long blockHeight);
    Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight);
    Task<ChainStatusDto> GetChainStatusAsync();
}