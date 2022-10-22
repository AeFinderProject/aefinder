using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task ScanBlockAsync();
}