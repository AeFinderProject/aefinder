using AeFinder.Grains.State.ApiTraffic;

namespace AeFinder.Grains.Grain.ApiTraffic;

public class ApiTrafficGrain : AeFinderGrain<ApiTrafficState>, IApiTrafficGrain
{
    public async Task IncreaseRequestCountAsync(long count)
    {
        await ReadStateAsync();
        State.RequestCount += count;
        await WriteStateAsync();
    }

    public async Task<long> GetRequestCountAsync()
    {
        await ReadStateAsync();
        return State.RequestCount;
    }
}