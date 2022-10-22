using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public interface IClientGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscribeInfo> GetSubscribeInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task<string> InitAsync(string chainId, string clientId, SubscribeInfo info);
}