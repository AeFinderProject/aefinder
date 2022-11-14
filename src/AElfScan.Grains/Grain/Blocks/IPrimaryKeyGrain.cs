using Orleans;

namespace AElfScan.Grains.Grain.Blocks;

public interface IPrimaryKeyGrain:IGrainWithStringKey
{
    Task SetCounter(int blocksCount);
    Task<string> GetCurrentGrainPrimaryKey(string chainId);
    Task<string> GetGrainPrimaryKey(string chainId);
}