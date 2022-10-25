using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public interface IClientManagerGrain : IGrainWithIntegerKey
{
    Task<List<string>> GetClientIdsAsync(string chainId);
    Task AddClientAsync(string chainId, string clientId);
}