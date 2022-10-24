using Orleans;

namespace AElfScan.Grain.Contracts.ScanClients;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleNewBlockAsync(Block block);
    Task InitializeAsync(string chainId, string clientId, string version);
}