using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task HandleHistoricalBlockAsync();
    Task HandleNewBlockAsync(Block block);
    Task<Guid> InitializeAsync(string chainId, string clientId, string version);
}