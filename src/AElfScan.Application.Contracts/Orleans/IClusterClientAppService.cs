using System.Threading.Tasks;
using Orleans;

namespace AElfScan.Orleans;

public interface IClusterClientAppService
{
    IClusterClient Client { get; }
    Task StartAsync();
    Task StopAsync();
}