using System;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiTraffic;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.ApiTraffic;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiTrafficService : AeFinderAppService, IApiTrafficService
{
    private readonly IApiTrafficProvider _apiTrafficProvider;
    private readonly IClusterClient _clusterClient;

    public ApiTrafficService(IApiTrafficProvider apiTrafficProvider, IClusterClient clusterClient)
    {
        _apiTrafficProvider = apiTrafficProvider;
        _clusterClient = clusterClient;
    }

    public async Task IncreaseRequestCountAsync(string key)
    {
        await _apiTrafficProvider.IncreaseRequestCountAsync(key);
    }

    public async Task<long> GetRequestCountAsync(string key, DateTime dateTime)
    {
        var apiTrafficGrain =
            _clusterClient.GetGrain<IApiTrafficGrain>(GrainIdHelper.GenerateApiTrafficGrainId(key, dateTime));
        return await apiTrafficGrain.GetRequestCountAsync();
    }
}