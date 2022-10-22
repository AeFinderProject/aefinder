using System.Threading.Tasks;
using Orleans;

namespace AElfScan.Chains;

public interface IChainGrain : IGrainWithStringKey
{
    Task SetLatestBlockAsync(string blockHash, long blockHeight);
    Task SetLatestConfirmBlockAsync(string blockHash, long blockHeight);
    Task<ChainStatusDto> GetChainStatusAsync();
}