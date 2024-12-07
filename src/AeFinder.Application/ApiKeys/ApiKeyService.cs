using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace AeFinder.ApiKeys;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiKeyService : AeFinderAppService, IApiKeyService
{
    private readonly IApiKeyTrafficProvider _apiKeyTrafficProvider;
    private readonly IApiKeyInfoProvider _apiKeyInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<ApiKeyIndex, Guid> _apiKeyIndexRepository;
    private readonly IEntityMappingRepository<ApiKeyQueryAeIndexerIndex, string> _apiKeyQueryAeIndexerIndexRepository;
    private readonly IEntityMappingRepository<ApiKeyQueryBasicApiIndex, string> _apiKeyQueryBasicApiIndexRepository;
    private readonly IEntityMappingRepository<ApiKeySummaryIndex, string> _apiKeySummaryIndexRepository;
    private readonly IApiKeySnapshotService _apiKeySnapshotService;

    public ApiKeyService(IApiKeyTrafficProvider apiKeyTrafficProvider, IClusterClient clusterClient,
        IApiKeyInfoProvider apiKeyInfoProvider, IEntityMappingRepository<ApiKeyIndex, Guid> apiKeyIndexRepository,
        IEntityMappingRepository<ApiKeyQueryAeIndexerIndex, string> apiKeyQueryAeIndexerIndexRepository,
        IEntityMappingRepository<ApiKeyQueryBasicApiIndex, string> apiKeyQueryBasicApiIndexRepository,
        IEntityMappingRepository<ApiKeySummaryIndex, string> apiKeySummaryIndexRepository,
        IApiKeySnapshotService apiKeySnapshotService)
    {
        _apiKeyTrafficProvider = apiKeyTrafficProvider;
        _clusterClient = clusterClient;
        _apiKeyInfoProvider = apiKeyInfoProvider;
        _apiKeyIndexRepository = apiKeyIndexRepository;
        _apiKeyQueryAeIndexerIndexRepository = apiKeyQueryAeIndexerIndexRepository;
        _apiKeyQueryBasicApiIndexRepository = apiKeyQueryBasicApiIndexRepository;
        _apiKeySummaryIndexRepository = apiKeySummaryIndexRepository;
        _apiKeySnapshotService = apiKeySnapshotService;
    }

    public async Task IncreaseQueryAeIndexerCountAsync(string key, string appId, string domain, DateTime dateTime)
    {
        await _apiKeyTrafficProvider.IncreaseAeIndexerQueryAsync(key, appId, domain, dateTime);
    }

    public async Task IncreaseQueryBasicApiCountAsync(string key, BasicApi api, string domain, DateTime dateTime)
    {
        await _apiKeyTrafficProvider.IncreaseBasicApiQueryAsync(key, api, domain, dateTime);
    }

    public async Task UpdateApiKeyInfoCacheAsync(ApiKeyInfo apiKeyInfo)
    {
        await _apiKeyInfoProvider.SetApiKeyInfoAsync(apiKeyInfo);
    }

    public async Task UpdateApiKeySummaryLimitCacheAsync(Guid organizationId, long limit)
    {
        await _apiKeyInfoProvider.SetApiKeySummaryLimitAsync(organizationId, limit);
    }

    public async Task UpdateApiKeySummaryUsedCacheAsync(Guid organizationId, DateTime dateTime, long used)
    {
        await _apiKeyInfoProvider.SetApiKeySummaryUsedAsync(organizationId, dateTime, used);
    }

    public async Task UpdateApiKeyUsedCacheAsync(Guid apiKeyId, DateTime dateTime, long used)
    {
        await _apiKeyInfoProvider.SetApiKeyUsedAsync(apiKeyId, dateTime, used);
    }

    public async Task AddOrUpdateApiKeyIndexAsync(ApiKeyChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyChangedEto, ApiKeyIndex>(input);
        await _apiKeyIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeyQueryAeIndexerIndexAsync(ApiKeyQueryAeIndexerChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryAeIndexerChangedEto, ApiKeyQueryAeIndexerIndex>(input);
        await _apiKeyQueryAeIndexerIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeyQueryBasicApiIndexAsync(ApiKeyQueryBasicApiChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryBasicApiChangedEto, ApiKeyQueryBasicApiIndex>(input);
        await _apiKeyQueryBasicApiIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeySummaryIndexAsync(ApiKeySummaryChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeySummaryChangedEto, ApiKeySummaryIndex>(input);
        await _apiKeySummaryIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<ApiKeySummaryDto> GetApiKeySummaryAsync(Guid organizationId)
    {
        var summary = await _apiKeySummaryIndexRepository.GetAsync(GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
        var dto = ObjectMapper.Map<ApiKeySummaryIndex, ApiKeySummaryDto>(summary);
        
        var monthDate = Clock.Now.ToMonthDate();
        var apiKeyMonthSnapshot = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(organizationId,
            new GetSnapshotInput
            {
                Type = SnapshotType.Monthly,
                BeginTime = monthDate,
                EndTime = monthDate
            });

        if (apiKeyMonthSnapshot.Items.Any())
        {
            dto.Query = apiKeyMonthSnapshot.Items.First().Query;
        }

        return dto;
    }

    public async Task<ApiKeyDto> CreateApiKeyAsync(Guid organizationId, CreateApiKeyInput input)
    {
        var newApiKeyId = Guid.NewGuid();
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
        var apiKeyInfo = await apiKeySummaryGrain.CreateApiKeyAsync(newApiKeyId, organizationId, input);

        var dto = ObjectMapper.Map<ApiKeyInfo, ApiKeyDto>(apiKeyInfo);
        dto.IsActive = true;
        return dto;
    }

    public async Task<ApiKeyDto> UpdateApiKeyAsync(Guid organizationId, Guid apiKeyId, UpdateApiKeyInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);
        var apiKeyInfo = await apikeyGrain.UpdateAsync(input);
        
        return ObjectMapper.Map<ApiKeyInfo, ApiKeyDto>(apiKeyInfo);
    }

    public async Task<ApiKeyDto> GetApiKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        var queryable = await _apiKeyIndexRepository.GetQueryableAsync();
        var apiKey = queryable.FirstOrDefault(o => o.OrganizationId == organizationId && o.Id == apiKeyId);

        var dto = ObjectMapper.Map<ApiKeyIndex, ApiKeyDto>(apiKey);

        var monthDate = Clock.Now.ToMonthDate();
        var apiKeyMonthSnapshot = await _apiKeySnapshotService.GetApiKeySnapshotsAsync(organizationId,
            apiKeyId,
            new GetSnapshotInput
            {
                Type = SnapshotType.Monthly,
                BeginTime = monthDate,
                EndTime = monthDate
            });

        dto.IsActive = true;
        if (apiKeyMonthSnapshot.Items.Any())
        {
            dto.PeriodQuery = apiKeyMonthSnapshot.Items.First().Query;
            if (dto.IsEnableSpendingLimit &&
                (long)(dto.SpendingLimitUsdt / AeFinderApplicationConsts.ApiKeyQueryPrice) - dto.PeriodQuery <= 0)
            {
                dto.IsActive = false;
            }
        }

        return dto;
    }

    public async Task<PagedResultDto<ApiKeyDto>> GetApiKeysAsync(Guid organizationId, GetApiKeyInput input)
    {
        var queryable = await _apiKeyIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId);
        var count = queryable.Count();
        var indices = queryable.OrderBy(o => o.Name).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        
        var monthDate = Clock.Now.ToMonthDate();
        var apiKeyMonthSnapshot = (await _apiKeySnapshotService.GetApiKeySnapshotsAsync(organizationId,null,
            new GetSnapshotInput
            {
                Type = SnapshotType.Monthly,
                BeginTime = monthDate,
                EndTime = monthDate
            })).Items.ToDictionary(o => o.ApiKeyId, o => o);
        
        var dtos = ObjectMapper.Map<List<ApiKeyIndex>, List<ApiKeyDto>>(indices);
        foreach (var dto in dtos)
        {
            dto.IsActive = true;
            if (apiKeyMonthSnapshot.TryGetValue(dto.Id, out var snapshotDto))
            {
                dto.PeriodQuery = snapshotDto.Query;
                if (dto.IsEnableSpendingLimit &&
                    (long)(dto.SpendingLimitUsdt / AeFinderApplicationConsts.ApiKeyQueryPrice) - snapshotDto.Query <= 0)
                {
                    dto.IsActive = false;
                }
            }
        }

        return new PagedResultDto<ApiKeyDto>
        {
            Items = dtos,
            TotalCount = count
        };
    }
    public async Task<RegenerateKeyDto> RegenerateKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        var newKey = await apikeyGrain.RegenerateKeyAsync();
        return new RegenerateKeyDto
        {
            Key = newKey
        };
    }

    public async Task DeleteApiKeyAsync(Guid organizationId, Guid apiKeyId)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);
        
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
        await apiKeySummaryGrain.DeleteApiKeyAsync(apiKeyId);
    }

    public async Task SetAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        await apikeyGrain.SetAuthorisedAeIndexersAsync(input.AppIds);
    }

    public async Task DeleteAuthorisedAeIndexersAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedAeIndexerInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        await apikeyGrain.DeleteAuthorisedAeIndexersAsync(input.AppIds);
    }

    public async Task SetAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        await apikeyGrain.SetAuthorisedDomainsAsync(input.Domains);
    }

    public async Task DeleteAuthorisedDomainsAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedDomainInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        await apikeyGrain.DeleteAuthorisedDomainsAsync(input.Domains);
    }

    public async Task SetAuthorisedApisAsync(Guid organizationId, Guid apiKeyId, SetAuthorisedApiInput input)
    {
        var apikeyGrain = _clusterClient.GetGrain<IApiKeyGrain>(apiKeyId);
        await CheckApiKeyAsync(apikeyGrain, organizationId);

        await apikeyGrain.SetAuthorisedApisAsync(input.Apis);
    }

    public async Task<PagedResultDto<ApiKeyQueryAeIndexerDto>> GetApiKeyQueryAeIndexersAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryAeIndexerInput input)
    {
        var queryable = await _apiKeyQueryAeIndexerIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId && o.ApiKeyId == apiKeyId);
        if (!input.AppId.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(o => o.AppId == input.AppId);
        }

        var count = queryable.Count();
        var indices = queryable.OrderBy(o => o.AppName).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<ApiKeyQueryAeIndexerDto>
        {
            Items = ObjectMapper.Map<List<ApiKeyQueryAeIndexerIndex>, List<ApiKeyQueryAeIndexerDto>>(indices),
            TotalCount = count
        };
    }

    public async Task<PagedResultDto<ApiKeyQueryApiDto>> GetApiKeyQueryApisAsync(Guid organizationId, Guid apiKeyId, GetApiKeyQueryApiInput input)
    {
        var queryable = await _apiKeyQueryBasicApiIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId && o.ApiKeyId == apiKeyId);
        if (input.Api.HasValue)
        {
            queryable = queryable.Where(o => o.Api == (int)input.Api.Value);
        }

        var count = queryable.Count();
        var indices = queryable.OrderBy(o => o.Api).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<ApiKeyQueryApiDto>
        {
            Items = ObjectMapper.Map<List<ApiKeyQueryBasicApiIndex>, List<ApiKeyQueryApiDto>>(indices),
            TotalCount = count
        };
    }

    public async Task AdjustQueryLimitAsync(Guid organizationId, long count)
    {
        var apiKeySummaryGrain =
            _clusterClient.GetGrain<IApiKeySummaryGrain>(GrainIdHelper.GenerateApiKeySummaryGrainId(organizationId));
        await apiKeySummaryGrain.AdjustQueryLimitAsync(organizationId, count);
    }

    public async Task<long> GetMonthQueryCountAsync(Guid organizationId, DateTime time)
    {
        var apiKeyMonthSnapshot = await _apiKeySnapshotService.GetApiKeySummarySnapshotsAsync(organizationId,
            new GetSnapshotInput
            {
                Type = SnapshotType.Monthly,
                BeginTime = time.ToMonthDate(),
                EndTime = time.ToMonthDate(),
            });
        return apiKeyMonthSnapshot.Items.Count > 0 ? apiKeyMonthSnapshot.Items[0].Query : 0;
    }

    private async Task CheckApiKeyAsync(IApiKeyGrain grain, Guid organizationId)
    {
        var apiKeyInfo = await grain.GetAsync();
        
        if (apiKeyInfo == null || apiKeyInfo.Name.IsNullOrWhiteSpace())
        {
            throw new EntityNotFoundException();
        }
        
        if (apiKeyInfo.OrganizationId != organizationId)
        {
            throw new UserFriendlyException("no permission.");
        }
    }
}