using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleNewBlockAsync(Block block);
    Task InitializeAsync(string chainId, string clientId, string version);
}