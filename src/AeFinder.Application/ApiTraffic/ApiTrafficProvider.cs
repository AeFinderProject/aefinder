using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiTraffic;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace AeFinder.ApiTraffic;

public interface IApiTrafficProvider
{
    Task IncreaseRequestCountAsync(string key);
    Task FlushAsync();
}

public class ApiTrafficProvider : IApiTrafficProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, long> _requestCount = new();
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;

    public ApiTrafficProvider(IClusterClient clusterClient, IClock clock)
    {
        _clusterClient = clusterClient;
        _clock = clock;
    }

    public Task IncreaseRequestCountAsync(string key)
    {
        var id = GrainIdHelper.GenerateApiTrafficGrainId(key, _clock.Now);
        _requestCount.AddOrUpdate(id, 1, (s, i) => i + 1);
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        foreach (var item in _requestCount)
        {
            if (item.Value == 0)
            {
                if (item.Key.Split('-')[1] != _clock.Now.ToString("yyyyMM"))
                {
                    _requestCount.TryRemove(item.Key, out _);
                }

                continue;
            }

            var apiTrafficGrain = _clusterClient.GetGrain<IApiTrafficGrain>(item.Key);
            await apiTrafficGrain.IncreaseRequestCountAsync(item.Value);

            _requestCount[item.Key] = 0;
        }
    }
}