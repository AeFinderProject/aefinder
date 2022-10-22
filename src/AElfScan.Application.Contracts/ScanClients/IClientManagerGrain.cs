using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public interface IClientManagerGrain : IGrainWithIntegerKey
{
    Task<List<string>> GetClientIdsAsync(string chainId);
    Task AddClientAsync(string chainId, string clientId);
}