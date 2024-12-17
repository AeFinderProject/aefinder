using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Market;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Timing;

namespace AeFinder.ApiKeys;

public interface IApiKeyTrafficProvider
{
    Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain, DateTime dateTime);
    Task IncreaseBasicApiQueryAsync(string apiKey, BasicApi api, string domain, DateTime dateTime);
    Task FlushAsync();
}

public class ApiKeyTrafficProvider : IApiKeyTrafficProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, AeIndexerApiTrafficSegment> _aeIndexerApiTraffics = new();
    private readonly ConcurrentDictionary<string, BasicApiTrafficSegment> _basicApiTraffics = new();
    private decimal _apiQueryPrice = 0;
    
    private readonly ApiKeyOptions _apiKeyOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IClock _clock;
    private readonly IApiKeyInfoProvider _apiKeyInfoProvider;
    private readonly IProductService _productService;

    public ApiKeyTrafficProvider(IClusterClient clusterClient, IClock clock,
        IOptionsSnapshot<ApiKeyOptions> apiKeyOptions, IApiKeyInfoProvider apiKeyInfoProvider,
        IProductService productService)
    {
        _clusterClient = clusterClient;
        _clock = clock;
        _apiKeyInfoProvider = apiKeyInfoProvider;
        _productService = productService;
        _apiKeyOptions = apiKeyOptions.Value;

        _apiQueryPrice = AsyncHelper.RunSync(async () => await GetApiKeyQueryPriceAsync());
    }

    public async Task IncreaseAeIndexerQueryAsync(string apiKey, string appId, string domain, DateTime dateTime)
    {
        var apiKeyInfo = await _apiKeyInfoProvider.GetApiKeyInfoAsync(apiKey);
        await CheckApiKeyAsync(apiKeyInfo, domain,appId,null,dateTime);
        await IncreaseAeIndexerQueryAsync(apiKeyInfo, appId, dateTime);
    }

    public async Task IncreaseBasicApiQueryAsync(string apiKey, BasicApi api, string domain, DateTime dateTime)
    {
        var apiKeyInfo = await _apiKeyInfoProvider.GetApiKeyInfoAsync(apiKey);
        await CheckApiKeyAsync(apiKeyInfo, domain,null,api,dateTime);
        await IncreaseBasicApiQueryAsync(apiKeyInfo, api, dateTime);
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
        
        foreach (var item in _basicApiTraffics)
        {
            if (item.Value.SegmentTime >= segmentTime)
            {
                continue;
            }

            if (!_basicApiTraffics.TryRemove(item.Key, out var value))
            {
                continue;
            }

            var apiKeyGrain =
                _clusterClient.GetGrain<IApiKeySummaryGrain>(
                    GrainIdHelper.GenerateApiKeySummaryGrainId(value.OrganizationId));
            await apiKeyGrain.RecordQueryBasicApiCountAsync(value.ApiKeyId, value.Api, value.Query,
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
    
    private async Task IncreaseBasicApiQueryAsync(ApiKeyInfo apiKeyInfo, BasicApi api, DateTime dateTime)
    {
        var segmentTime = GetSegmentTime(dateTime);
        var key = GetBasicApiTrafficKey(apiKeyInfo.Id, api, segmentTime);
        _basicApiTraffics.AddOrUpdate(key, new BasicApiTrafficSegment()
        {
            SegmentTime = segmentTime,
            OrganizationId = apiKeyInfo.OrganizationId,
            ApiKeyId = apiKeyInfo.Id,
            Api = api,
            Query = 1,
            LastQueryTime = dateTime
        }, (s, i) =>
        {
            i.Query += 1;
            return i;
        });
    }

    private async Task CheckApiKeyAsync(ApiKeyInfo apiKeyInfo, string domain, string appId,
        BasicApi? api, DateTime dateTime)
    {
        var limit = await _apiKeyInfoProvider.GetApiKeySummaryLimitAsync(apiKeyInfo.OrganizationId);
        var usedSummary = await _apiKeyInfoProvider.GetApiKeySummaryUsedAsync(apiKeyInfo.OrganizationId, dateTime);
        var used = await _apiKeyInfoProvider.GetApiKeyUsedAsync(apiKeyInfo.Id, dateTime);
        if (usedSummary >= limit)
        {
            throw new UserFriendlyException("Api key query times insufficient.");
        }

        if (apiKeyInfo.IsEnableSpendingLimit && (long)(apiKeyInfo.SpendingLimitUsdt / _apiQueryPrice) - used <= 0)
        {
            throw new UserFriendlyException("Api key unavailable.");
        }

        if (!appId.IsNullOrWhiteSpace() && apiKeyInfo.AuthorisedAeIndexers.Any() && !apiKeyInfo.AuthorisedAeIndexers.ContainsKey(appId))
        {
            throw new UserFriendlyException("Unauthorized AeIndexer.");
        }

        if (api.HasValue && apiKeyInfo.AuthorisedApis.Any() &&
            !apiKeyInfo.AuthorisedApis.Contains(api.Value))
        {
            throw new UserFriendlyException("Unauthorized api.");
        }

        if (apiKeyInfo.AuthorisedDomains.Any() &&
            (domain.IsNullOrWhiteSpace() || !CheckApiKeyDomain(apiKeyInfo, domain)))
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
    
    private string GetBasicApiTrafficKey(Guid apiKeyId, BasicApi api, DateTime dateTime)
    {
        return $"{apiKeyId:N}-{api}-{dateTime}";
    }

    private DateTime GetSegmentTime(DateTime dateTime)
    {
        var minute = (dateTime.Minute / _apiKeyOptions.FlushPeriod) * _apiKeyOptions.FlushPeriod;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minute, 0, _clock.Kind);
    }
    
    private async Task<decimal> GetApiKeyQueryPriceAsync()
    {
        var apiQueryProduct = await _productService.GetRegularApiQueryCountProductInfoAsync();
        return apiQueryProduct.MonthlyUnitPrice / apiQueryProduct.QueryCount;
    }
}