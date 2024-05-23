using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.GraphQL.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AeFinder.GraphQL;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GraphQLAppService : AeFinderAppService, IGraphQLAppService, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IDistributedCache<string, string> _appCurrentVersionCache;
    private readonly ILogger<GraphQLAppService> _logger;

    public GraphQLAppService(IHttpClientFactory httpClientFactory,
        ILogger<GraphQLAppService> logger, 
        IBlockScanAppService blockScanAppService,
        IDistributedCache<string, string> appCurrentVersionCache)
    {
        _httpClientFactory = httpClientFactory;
        _blockScanAppService = blockScanAppService;
        _appCurrentVersionCache = appCurrentVersionCache;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> RequestForwardAsync(string appId, string version,
        string kubernetesOriginName, GraphQLQueryInput input)
    {
        var serverUrl = "";
        string originName = kubernetesOriginName.TrimEnd('/');

        string currentVersion = await EnsureCurrentVersion(appId, version);
        if (currentVersion == null)
        {
            var exceptionResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    "No available current version, please contact admin check subscription info.")
            };
            return exceptionResponse;
        }

        serverUrl = $"{originName}/{appId}/{currentVersion}/graphql";

        _logger.LogInformation("RequestForward:" + serverUrl);
        var json = JsonSerializer.Serialize(new { query = input.Query, variables = input.Variables });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(serverUrl, content);

        return response;
    }

    private async Task<string> EnsureCurrentVersion(string appId, string version)
    {
        if (!version.IsNullOrEmpty())
        {
            return version;
        }

        // Try to get the current version from the cache
        var currentVersion = await GetAppCurrentVersionCacheAsync(appId);
        if (!currentVersion.IsNullOrEmpty())
        {
            return currentVersion;
        }

        // Get subscription information
        var allSubscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        if (allSubscription?.CurrentVersion == null || allSubscription.CurrentVersion.Version.IsNullOrEmpty())
        {
            return null;
        }

        currentVersion = allSubscription.CurrentVersion.Version;
        // Update cache
        await CacheAppCurrentVersionAsync(appId, currentVersion);

        return currentVersion;
    }

    public async Task CacheAppCurrentVersionAsync(string appId, string currentVersion)
    {
        var cacheKey = await GetAppCurrentVersionCacheNameAsync(appId);
        await _appCurrentVersionCache.SetAsync(cacheKey, currentVersion, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(AeFinderApplicationConsts.AppCurrentVersionCacheHours)
        });
    }

    public async Task<string> GetAppCurrentVersionCacheAsync(string appId)
    {
        var cacheKey = await GetAppCurrentVersionCacheNameAsync(appId);
        return await _appCurrentVersionCache.GetAsync(cacheKey);;
    }

    public async Task<string> GetAppCurrentVersionCacheNameAsync(string appId)
    {
        return AeFinderApplicationConsts.AppCurrentVersionCacheKeyPrefix + appId;
    }
}