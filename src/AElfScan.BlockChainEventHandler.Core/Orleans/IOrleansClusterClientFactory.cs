using System.Threading.Tasks;
using Orleans;

namespace AElfScan.Orleans;

public interface IOrleansClusterClientFactory
{
    Task<IClusterClient> GetClient();
}