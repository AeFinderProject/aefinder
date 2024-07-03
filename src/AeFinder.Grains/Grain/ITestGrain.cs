using Orleans;

namespace AeFinder.Grains.Grain;

public interface ITestGrain : IGrainWithStringKey
{
    Task CountAsync();
    Task<int> GetCountAsync();
}