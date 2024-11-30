using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AeFinder.ApiKeys;

public interface IApiKeyInfoProvider
{
    Task<ApiKeyInfo> GetApiKeyInfoAsync(Guid apiKeyId);
    Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey);
    Task SetApiKeyInfoAsync(ApiKeyInfo apiKeyInfo);
    Task<long> GetApiKeyLimitAsync(Guid organizationId);
    Task SetApiKeyLimitAsync(Guid organizationId, long limit);
    Task<long> GetApiKeyUsedAsync(Guid organizationId, DateTime dateTime);
    Task SetApiKeyUsedAsync(Guid organizationId, long used);
}

public class ApiKeyInfoProvider : IApiKeyInfoProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<Guid, ApiKeyInfo> _apiKeys = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeyIdMapping = new();
    private readonly ConcurrentDictionary<Guid, long> _apiKeyLimit = new();
    private readonly ConcurrentDictionary<Guid, long> _apiKeyUsed = new();

    private readonly IClusterClient _clusterClient;

    public ApiKeyInfoProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public async Task<ApiKeyInfo> GetApiKeyInfoAsync(Guid apiKeyId)
    {
        if (!_apiKeys.TryGetValue(apiKeyId, out var info))
        {
            var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
            info = await apiKeyGrain.GetAsync();

            if (info == null || info.Key.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Api key does not exist.");
            }

            _apiKeys[apiKeyId] = info;
        }

        return info;
    }

    public async Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey)
    {
        if (!_apiKeyIdMapping.TryGetValue(apiKey, out var id))
        {
            // TODO: Get id from es
            id = Guid.Empty;
            _apiKeyIdMapping[apiKey] = id;
        }

        return await GetApiKeyInfoAsync(id);
    }

    public Task SetApiKeyInfoAsync(ApiKeyInfo apiKeyInfo)
    {
        if (_apiKeys.TryGetValue(apiKeyInfo.Id, out var oldApiKeyInfo) && oldApiKeyInfo.Key != apiKeyInfo.Key)
        {
            _apiKeyIdMapping.Remove(oldApiKeyInfo.Key, out _);
        }

        _apiKeyIdMapping[apiKeyInfo.Key] = apiKeyInfo.Id;
        _apiKeys[apiKeyInfo.Id] = apiKeyInfo;
        return Task.CompletedTask;
    }

    public async Task<long> GetApiKeyLimitAsync(Guid organizationId)
    {
        if (!_apiKeyLimit.TryGetValue(organizationId, out var value))
        {
            var apiKeyGrain =
                _clusterClient.GetGrain<IApiKeySummaryGrain>(
                    GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
            value = (await apiKeyGrain.GetApiKeySummaryInfoAsync()).QueryLimit;
            _apiKeyLimit[organizationId] = value;
        }

        return value;
    }

    public Task SetApiKeyLimitAsync(Guid organizationId, long limit)
    {
        _apiKeyLimit[organizationId] = limit;
        return Task.CompletedTask;
    }

    public async Task<long> GetApiKeyUsedAsync(Guid organizationId, DateTime dateTime)
    {
        if (!_apiKeyUsed.TryGetValue(organizationId, out var value))
        {
            var monthlySnapshotKey =
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(organizationId, dateTime);
            var monthlySnapshotGrain = _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(monthlySnapshotKey);
            value = await monthlySnapshotGrain.GetQueryCountAsync();
            _apiKeyUsed[organizationId] = value;
        }

        return value;
    }

    public Task SetApiKeyUsedAsync(Guid organizationId, long used)
    {
        _apiKeyUsed[organizationId] = used;
        return Task.CompletedTask;
    }
}