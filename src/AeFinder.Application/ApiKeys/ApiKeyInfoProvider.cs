using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AeFinder.ApiKeys;

public interface IApiKeyInfoProvider
{
    Task<ApiKeyInfo> GetApiKeyInfoAsync(Guid apiKeyId);
    Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey);
    Task SetApiKeyInfoAsync(ApiKeyInfo apiKeyInfo);
    Task<long> GetApiKeySummaryLimitAsync(Guid organizationId);
    Task SetApiKeySummaryLimitAsync(Guid organizationId, long limit);
    Task<long> GetApiKeySummaryUsedAsync(Guid organizationId, DateTime dateTime);
    Task SetApiKeySummaryUsedAsync(Guid organizationId, DateTime dateTime, long used);
    Task<long> GetApiKeyUsedAsync(Guid apiKeyId, DateTime dateTime);
    Task SetApiKeyUsedAsync(Guid apiKeyId, DateTime dateTime, long used);
}

public class ApiKeyInfoProvider : IApiKeyInfoProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<Guid, ApiKeyInfo> _apiKeys = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeyIdMapping = new();
    private readonly ConcurrentDictionary<Guid, long> _apiKeySummaryLimit = new();
    private readonly ConcurrentDictionary<string, long> _apiKeySummaryUsed = new();
    private readonly ConcurrentDictionary<string, long> _apiKeyUsed = new();

    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<ApiKeyIndex, Guid> _apiKeyIndexRepository;

    public ApiKeyInfoProvider(IClusterClient clusterClient,
        IEntityMappingRepository<ApiKeyIndex, Guid> apiKeyIndexRepository)
    {
        _clusterClient = clusterClient;
        _apiKeyIndexRepository = apiKeyIndexRepository;
    }

    public async Task<ApiKeyInfo> GetApiKeyInfoAsync(Guid apiKeyId)
    {
        if (!_apiKeys.TryGetValue(apiKeyId, out var info))
        {
            var apiKeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
            info = await apiKeyGrain.GetAsync();

            if (info == null || info.Key.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("Api key not exist.");
            }

            _apiKeys[apiKeyId] = info;
        }

        return info;
    }

    public async Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey)
    {
        if (!_apiKeyIdMapping.TryGetValue(apiKey, out var id))
        {
            var queryable = await _apiKeyIndexRepository.GetQueryableAsync();
            var index = queryable.FirstOrDefault(o => o.Key == apiKey);
            if (index == null)
            {
                throw new UserFriendlyException("Api key not exist.");
            }

            id = index.Id;
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

    public async Task<long> GetApiKeySummaryLimitAsync(Guid organizationId)
    {
        if (!_apiKeySummaryLimit.TryGetValue(organizationId, out var value))
        {
            var apiKeyGrain =
                _clusterClient.GetGrain<IApiKeySummaryGrain>(
                    GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
            value = (await apiKeyGrain.GetApiKeySummaryInfoAsync()).QueryLimit;
            _apiKeySummaryLimit[organizationId] = value;
        }

        return value;
    }

    public Task SetApiKeySummaryLimitAsync(Guid organizationId, long limit)
    {
        _apiKeySummaryLimit[organizationId] = limit;
        return Task.CompletedTask;
    }

    public async Task<long> GetApiKeySummaryUsedAsync(Guid organizationId, DateTime dateTime)
    {
        var key = GetApiSummaryUsedKey(organizationId, dateTime);
        if (!_apiKeySummaryUsed.TryGetValue(key, out var value))
        {
            var monthlySnapshotKey =
                GrainIdHelper.GenerateApiKeySummaryMonthlySnapshotGrainId(organizationId, dateTime);
            var monthlySnapshotGrain = _clusterClient.GetGrain<IApiKeySummarySnapshotGrain>(monthlySnapshotKey);
            value = await monthlySnapshotGrain.GetQueryCountAsync();
            _apiKeySummaryUsed[key] = value;
        }

        return value;
    }

    public Task SetApiKeySummaryUsedAsync(Guid organizationId, DateTime dateTime, long used)
    {
        var key = GetApiSummaryUsedKey(organizationId, dateTime);
        _apiKeySummaryUsed[key] = used;
        return Task.CompletedTask;
    }

    public async Task<long> GetApiKeyUsedAsync(Guid apiKeyId, DateTime dateTime)
    {
        var key = GetApiUsedKey(apiKeyId, dateTime);
        if (!_apiKeyUsed.TryGetValue(key, out var value))
        {
            var monthlySnapshotKey =
                GrainIdHelper.GenerateApiKeyMonthlySnapshotGrainId(apiKeyId, dateTime);
            var monthlySnapshotGrain = _clusterClient.GetGrain<IApiKeySnapshotGrain>(monthlySnapshotKey);
            value = await monthlySnapshotGrain.GetQueryCountAsync();
            _apiKeyUsed[key] = value;
        }

        return value;
    }

    public Task SetApiKeyUsedAsync(Guid apiKeyId, DateTime dateTime, long used)
    {
        var key = GetApiUsedKey(apiKeyId, dateTime);
        _apiKeyUsed[key] = used;
        return Task.CompletedTask;
    }

    private string GetApiSummaryUsedKey(Guid organizationId, DateTime dateTime)
    {
        return $"{organizationId:N}-{dateTime:yyyyMM}";
    }
    
    private string GetApiUsedKey(Guid apiKeyId, DateTime dateTime)
    {
        return $"{apiKeyId:N}-{dateTime:yyyyMM}";
    }
}