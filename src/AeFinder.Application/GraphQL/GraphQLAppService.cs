using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.GraphQL.Dto;
using AeFinder.Kubernetes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly KubernetesOptions _kubernetesOption;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IDistributedCache<string, string> _appCurrentVersionCache;
    private readonly ILogger<GraphQLAppService> _logger;

    public GraphQLAppService(IHttpClientFactory httpClientFactory,
        ILogger<GraphQLAppService> logger, IOptionsSnapshot<KubernetesOptions> kubernetesOption,
        IBlockScanAppService blockScanAppService,
        IDistributedCache<string, string> appCurrentVersionCache)
    {
        _httpClientFactory = httpClientFactory;
        _kubernetesOption = kubernetesOption.Value;
        _blockScanAppService = blockScanAppService;
        _appCurrentVersionCache = appCurrentVersionCache;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> RequestForwardAsync(string appId, string version, GraphQLQueryInput input)
    {
        var serverUrl = "";
        string originName = _kubernetesOption.OriginName.TrimEnd('/');
        if (!version.IsNullOrEmpty())
        {
            serverUrl = $"{originName}/{appId}/{version}/graphql";
        }
        else
        {
            //Get app current version
            var currentVersion = await GetAppCurrentVersionCacheAsync(appId);
            if (currentVersion.IsNullOrEmpty())
            {
                //get app current version from grain
                var allSubscription = await _blockScanAppService.GetSubscriptionAsync(appId);
                if (allSubscription == null || allSubscription.CurrentVersion == null)
                {
                    var exceptionResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("No available current version, please contact admin check subscription info.")
                    };
                    return exceptionResponse;
                }
                currentVersion = allSubscription.CurrentVersion.Version;
                if (currentVersion.IsNullOrEmpty())
                {
                    var exceptionResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("No current version, please contact admin check subscription info.")
                    };
                    return exceptionResponse;
                }
                //set app current version to cache
                await CacheAppCurrentVersionAsync(appId, currentVersion);
            }
            serverUrl = $"{originName}/{appId}/{currentVersion}/graphql";
        }

        _logger.LogInformation("RequestForward:" + serverUrl);
        var json = JsonSerializer.Serialize(new { query = input.Query, variables = input.Variables });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(serverUrl, content);

        return response;
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