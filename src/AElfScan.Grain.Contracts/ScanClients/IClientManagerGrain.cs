using Orleans;

namespace AElfScan.Grain.Contracts.ScanClients;

public interface IClientManagerGrain : IGrainWithIntegerKey
{
    Task<List<string>> GetClientIdsAsync(string chainId);
    Task AddClientAsync(string chainId, string clientId);
}