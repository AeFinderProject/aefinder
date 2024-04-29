using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.GraphQL.Dto;
using AeFinder.Kubernetes;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AeFinder.GraphQL;

public class GraphQLAppService : AeFinderAppService, IGraphQLAppService, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KubernetesOptions _kubernetesOption;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IDistributedCache<string, string> _appCurrentVersionCache;

    public GraphQLAppService(IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption,
        IBlockScanAppService blockScanAppService,
        IDistributedCache<string, string> appCurrentVersionCache)
    {
        _httpClientFactory = httpClientFactory;
        _kubernetesOption = kubernetesOption.Value;
        _blockScanAppService = blockScanAppService;
        _appCurrentVersionCache = appCurrentVersionCache;
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
                    throw new Exception("No available current version, please contact admin check subscription info");
                }
                currentVersion = allSubscription.CurrentVersion.Version;
                if (currentVersion.IsNullOrEmpty())
                {
                    throw new Exception("No current version, please contact admin check subscription info");
                }
                //set app current version to cache
                await CacheAppCurrentVersionAsync(appId, currentVersion);
            }
            serverUrl = $"{originName}/{appId}/{currentVersion}/graphql";
        }

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