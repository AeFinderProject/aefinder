using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Timing;

namespace AeFinder.ApiKeys;

public interface IApiKeyTrafficProvider
{
    Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain);
    Task IncreaseBasicDataQueryAsync(string apiKey, BasicDataApiType basicDataApiType, string domain);
    Task FlushAsync();
}

public class ApiKeyTrafficProvider : IApiKeyTrafficProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, AeIndexerApiTrafficSegment> _aeIndexerApiTraffics = new();
    private readonly ConcurrentDictionary<string, BasicDataApiTrafficSegment> _basicDataApiTraffics = new();
    
    private readonly ApiKeyOptions _apiKeyOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;
    private readonly IApiKeyInfoProvider _apiKeyInfoProvider;

    public ApiKeyTrafficProvider(IClusterClient clusterClient, IClock clock,
        IOptionsSnapshot<ApiKeyOptions> apiKeyOptions, IApiKeyInfoProvider apiKeyInfoProvider)
    {
        _clusterClient = clusterClient;
        _clock = clock;
        _apiKeyInfoProvider = apiKeyInfoProvider;
        _apiKeyOptions = apiKeyOptions.Value;
    }

    public async Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain)
    {
        var dateTime = _clock.Now;
        var apiKeyInfo = await _apiKeyInfoProvider.GetApiKeyInfoAsync(apiKey);
        await CheckApiKeyAsync(apiKeyInfo, domain,appId,null,dateTime);
        await IncreaseAeIndexerQueryAsync(apiKeyInfo, appId, dateTime);
    }

    public async Task IncreaseBasicDataQueryAsync(string apiKey, BasicDataApiType basicDataApiType, string domain)
    {
        var dateTime = _clock.Now;
        var apiKeyInfo = await _apiKeyInfoProvider.GetApiKeyInfoAsync(apiKey);
        await CheckApiKeyAsync(apiKeyInfo, domain,null,basicDataApiType,dateTime);
        await IncreaseBasicDataQueryAsync(apiKeyInfo, basicDataApiType, dateTime);
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

    private async Task CheckApiKeyAsync(ApiKeyInfo apiKeyInfo, string domain, string appId,
        BasicDataApiType? basicDataApiType, DateTime dateTime)
    {
        var limit = await _apiKeyInfoProvider.GetApiKeyLimitAsync(apiKeyInfo.OrganizationId);
        var used = await _apiKeyInfoProvider.GetApiKeyUsedAsync(apiKeyInfo.OrganizationId, dateTime);
        if (used >= limit)
        {
            throw new UserFriendlyException("Api key query times insufficient.");
        }

        if (apiKeyInfo.Status != ApiKeyStatus.Active)
        {
            throw new UserFriendlyException("Api key unavailable.");
        }

        if (!appId.IsNullOrWhiteSpace() && apiKeyInfo.AuthorisedAeIndexers.Any() && !apiKeyInfo.AuthorisedAeIndexers.Contains(appId))
        {
            throw new UserFriendlyException("Unauthorized AeIndexer.");
        }

        if (basicDataApiType.HasValue && apiKeyInfo.AuthorisedApis.Any() &&
            !apiKeyInfo.AuthorisedApis.Contains(basicDataApiType.Value))
        {
            throw new UserFriendlyException("Unauthorized api.");
        }

        if (apiKeyInfo.AuthorisedDomains.Any() && !CheckApiKeyDomain(apiKeyInfo, domain))
        {
            throw new UserFriendlyException("Unauthorized domain.");
        }
    }

    private bool CheckApiKeyDomain(ApiKeyInfo apiKeyInfo, string domain)
    {
        foreach (var authorisedDomain in apiKeyInfo.AuthorisedDomains)
        {
            if (authorisedDomain.StartsWith("*.") && (domain.EndsWith(authorisedDomain.RemovePreFix("*")) ||
                                                      domain == authorisedDomain.RemovePreFix("*.")))
            {
                return true;
            }

            if (authorisedDomain == domain)
            {
                return true;
            }
        }

        return false;
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