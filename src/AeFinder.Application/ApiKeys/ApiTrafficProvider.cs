using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace AeFinder.ApiKeys;

public interface IApiKeyTrafficProvider
{
    Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain, DateTime dateTime);
    Task IncreaseBasicDataQueryAsync(string apiKey, BasicDataApiType basicDataApiType, string domain, DateTime dateTime);
    Task FlushAsync();
}

public class ApiKeyTrafficProvider : IApiKeyTrafficProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, AeIndexerApiTrafficSegment> _aeIndexerApiTraffics = new();
    private readonly ConcurrentDictionary<string, BasicDataApiTrafficSegment> _basicDataApiTraffics = new();
    private readonly ConcurrentDictionary<Guid, ApiKeyInfo> _apiKeys = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeyIdMapping = new();
    private readonly ConcurrentDictionary<Guid, ApiKeyUsage> _apiKeyUsages = new();
    private readonly ApiKeyOptions _apiKeyOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;

    public ApiKeyTrafficProvider(IClusterClient clusterClient, IClock clock, IOptionsSnapshot<ApiKeyOptions> apiKeyOptions)
    {
        _clusterClient = clusterClient;
        _clock = clock;
        _apiKeyOptions = apiKeyOptions.Value;
    }

    public async Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain, DateTime dateTime)
    {
        var apiKeyId = await GetApiKeyIdAsync(apiKey);
    }

    public async Task IncreaseBasicDataQueryAsync(string apiKey, BasicDataApiType basicDataApiType, string domain, DateTime dateTime)
    {
        throw new NotImplementedException();
    }

    public async Task FlushAsync()
    {
        var segmentTime = GetSegmentTime(_clock.Now);
        foreach (var item in _aeIndexerApiTraffics)
        {
            if (item.Value.SegmentTime >= segmentTime)
            {
                continue;
            }

            if (!_aeIndexerApiTraffics.TryRemove(item.Key, out var value))
            {
                continue;
            }

            var apiKeyGrain =
                _clusterClient.GetGrain<IApiKeySummaryGrain>(
                    GrainIdHelper.GenerateApiKeySummaryGrainId(value.OrganizationId));
            await apiKeyGrain.RecordQueryAeIndexerCountAsync(value.ApiKeyId, value.AppId, value.Query,
                value.LastQueryTime);
        }
        
        foreach (var item in _basicDataApiTraffics)
        {
            if (item.Value.SegmentTime >= segmentTime)
            {
                continue;
            }

            if (!_basicDataApiTraffics.TryRemove(item.Key, out var value))
            {
                continue;
            }

            var apiKeyGrain =
                _clusterClient.GetGrain<IApiKeySummaryGrain>(
                    GrainIdHelper.GenerateApiKeySummaryGrainId(value.OrganizationId));
            await apiKeyGrain.RecordQueryBasicDataCountAsync(value.ApiKeyId, value.BasicDataApiType, value.Query,
                value.LastQueryTime);
        }
    }
    
    private Task IncreaseAeIndexerQueryAsync(ApiKeyInfo apiKeyInfo, string appId, DateTime dateTime)
    {
        var segmentTime = GetSegmentTime(dateTime);
        var key = GetAeIndexerApiTrafficKey(apiKeyInfo.Id, appId, segmentTime);
        _aeIndexerApiTraffics.AddOrUpdate(key, new AeIndexerApiTrafficSegment
        {
            SegmentTime = segmentTime,
            OrganizationId = apiKeyInfo.OrganizationId,
            ApiKeyId = apiKeyInfo.Id,
            AppId = appId,
            Query = 1,
            LastQueryTime = dateTime
        }, (s, i) =>
        {
            i.Query += 1;
            return i;
        });
        return Task.CompletedTask;
    }
    
    private async Task IncreaseBasicDataQueryAsync(ApiKeyInfo apiKeyInfo, BasicDataApiType basicDataApiType, DateTime dateTime)
    {
        var segmentTime = GetSegmentTime(dateTime);
        var key = GetBasicDataApiTrafficKey(apiKeyInfo.Id, basicDataApiType, segmentTime);
        _basicDataApiTraffics.AddOrUpdate(key, new BasicDataApiTrafficSegment()
        {
            SegmentTime = segmentTime,
            OrganizationId = apiKeyInfo.OrganizationId,
            ApiKeyId = apiKeyInfo.Id,
            BasicDataApiType = basicDataApiType,
            Query = 1,
            LastQueryTime = dateTime
        }, (s, i) =>
        {
            i.Query += 1;
            return i;
        });
    }

    private async Task VerifyAsync(Guid organizationId, Guid apiKeyId, string domain)
    {
        
    }

    private async Task<Guid> GetApiKeyIdAsync(string key)
    {
        if (_apiKeyIdMapping.TryGetValue(key, out var id))
        {
            // TODO: Get id from es
            id = Guid.Empty;
            _apiKeyIdMapping[key] = id;
        }

        return id;
    }

    private async Task<ApiKeyInfo> GetApiKeyInfoAsync(Guid apiKeyId)
    {
        if (_apiKeys.TryGetValue(apiKeyId, out var info))
        {
            var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
            info = await apiKeyGrain.GetAsync();
            
            _apiKeys[apiKeyId] = info;
        }

        return info;
    }

    private string GetAeIndexerApiTrafficKey(Guid apiKeyId, string appId, DateTime dateTime)
    {
        return $"{apiKeyId:N}-{appId}-{dateTime}";
    }
    
    private string GetBasicDataApiTrafficKey(Guid apiKeyId, BasicDataApiType basicDataApiType, DateTime dateTime)
    {
        return $"{apiKeyId:N}-{basicDataApiType}-{dateTime}";
    }

    private DateTime GetSegmentTime(DateTime dateTime)
    {
        var minute = (dateTime.Minute / _apiKeyOptions.FlushPeriod) * _apiKeyOptions.FlushPeriod;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minute, 0, _clock.Kind);
    }
}