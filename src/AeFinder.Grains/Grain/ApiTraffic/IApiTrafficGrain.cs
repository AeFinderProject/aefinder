namespace AeFinder.Grains.Grain.ApiTraffic;

public interface IApiTrafficGrain : IGrainWithStringKey
{
    Task IncreaseRequestCountAsync(long count);
    Task<long> GetRequestCountAsync();
}