using Orleans;

namespace AElfScan.Grains.Grain;

public interface IPrimaryKeyGrain:IGrainWithStringKey
{
    Task<string> GetCurrentGrainPrimaryKey(string chainId);
    Task<string> GetGrainPrimaryKey(string chainId, int blocksCount);
}