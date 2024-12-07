using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace AeFinder.ApiKeys;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiKeySnapshotService : AeFinderAppService, IApiKeySnapshotService
{
    private readonly IEntityMappingRepository<ApiKeyQueryAeIndexerSnapshotIndex, string>
        _apiKeyQueryAeIndexerSnapshotIndexRepository;

    private readonly IEntityMappingRepository<ApiKeyQueryBasicApiSnapshotIndex, string>
        _apiKeyQueryBasicApiIndexRepository;

    private readonly IEntityMappingRepository<ApiKeySnapshotIndex, string> _apiKeySnapshotIndexRepository;
    private readonly IEntityMappingRepository<ApiKeySummarySnapshotIndex, string> _apiKeySummarySnapshotIndexRepository;

    private const int MaxResultCount = 1000;

    public ApiKeySnapshotService(
        IEntityMappingRepository<ApiKeySnapshotIndex, string> apiKeySnapshotIndexRepository,
        IEntityMappingRepository<ApiKeySummarySnapshotIndex, string> apiKeySummarySnapshotIndexRepository,
        IEntityMappingRepository<ApiKeyQueryAeIndexerSnapshotIndex, string> apiKeyQueryAeIndexerSnapshotIndexRepository,
        IEntityMappingRepository<ApiKeyQueryBasicApiSnapshotIndex, string> apiKeyQueryBasicApiIndexRepository)
    {
        _apiKeySnapshotIndexRepository = apiKeySnapshotIndexRepository;
        _apiKeySummarySnapshotIndexRepository = apiKeySummarySnapshotIndexRepository;
        _apiKeyQueryAeIndexerSnapshotIndexRepository = apiKeyQueryAeIndexerSnapshotIndexRepository;
        _apiKeyQueryBasicApiIndexRepository = apiKeyQueryBasicApiIndexRepository;
    }

    public async Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryAeIndexerSnapshotChangedEto, ApiKeyQueryAeIndexerSnapshotIndex>(input);
        await _apiKeyQueryAeIndexerSnapshotIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeyQueryBasicApiSnapshotChangedEto, ApiKeyQueryBasicApiSnapshotIndex>(input);
        await _apiKeyQueryBasicApiIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeySnapshotChangedEto, ApiKeySnapshotIndex>(input);
        await _apiKeySnapshotIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task AddOrUpdateApiKeySummarySnapshotIndexAsync(ApiKeySummarySnapshotChangedEto input)
    {
        var index = ObjectMapper.Map<ApiKeySummarySnapshotChangedEto, ApiKeySummarySnapshotIndex>(input);
        await _apiKeySummarySnapshotIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<ListResultDto<ApiKeySummarySnapshotDto>> GetApiKeySummarySnapshotsAsync(Guid organizationId,
        GetSnapshotInput input)
    {
        var queryable = await _apiKeySummarySnapshotIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId && o.Type == (int)input.Type);

        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time >= input.BeginTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time <= input.EndTime.Value);
        }

        var indices = queryable.OrderBy(o => o.Time).Take(MaxResultCount).ToList();

        return new ListResultDto<ApiKeySummarySnapshotDto>
        {
            Items = ObjectMapper.Map<List<ApiKeySummarySnapshotIndex>, List<ApiKeySummarySnapshotDto>>(indices)
        };
    }

    public async Task<ListResultDto<ApiKeySnapshotDto>> GetApiKeySnapshotsAsync(Guid organizationId, Guid? apiKeyId,
        GetSnapshotInput input)
    {
        var queryable = await _apiKeySnapshotIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId && o.Type == (int)input.Type);
        if (apiKeyId.HasValue)
        {
            queryable = queryable.Where(o => o.ApiKeyId == apiKeyId.Value);
        }

        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time >= input.BeginTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time <= input.EndTime.Value);
        }

        var indices = queryable.OrderBy(o => o.Time).Take(MaxResultCount).ToList();

        return new ListResultDto<ApiKeySnapshotDto>
        {
            Items = ObjectMapper.Map<List<ApiKeySnapshotIndex>, List<ApiKeySnapshotDto>>(indices)
        };
    }

    public async Task<ListResultDto<ApiKeyQueryAeIndexerSnapshotDto>> GetApiKeyQueryAeIndexerSnapshotsAsync(
        Guid organizationId, Guid apiKeyId, GetQueryAeIndexerSnapshotInput input)
    {
        var queryable = await _apiKeyQueryAeIndexerSnapshotIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == organizationId && o.ApiKeyId == apiKeyId && o.Type == (int)input.Type);

        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time >= input.BeginTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time <= input.EndTime.Value);
        }

        if (!input.AppId.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(o => o.AppId == input.AppId);
        }

        var indices = queryable.OrderBy(o => o.Time).Take(MaxResultCount).ToList();

        return new ListResultDto<ApiKeyQueryAeIndexerSnapshotDto>
        {
            Items =
                ObjectMapper.Map<List<ApiKeyQueryAeIndexerSnapshotIndex>, List<ApiKeyQueryAeIndexerSnapshotDto>>(
                    indices)
        };
    }

    public async Task<ListResultDto<ApiKeyQueryBasicApiSnapshotDto>> GetApiKeyQueryBasicApiSnapshotsAsync(
        Guid organizationId, Guid apiKeyId, GetQueryBasicApiSnapshotInput input)
    {
        var queryable = await _apiKeyQueryBasicApiIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == organizationId && o.ApiKeyId == apiKeyId && o.Type == (int)input.Type);

        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time >= input.BeginTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.Time <= input.EndTime.Value);
        }

        if (input.Api.HasValue)
        {
            queryable = queryable.Where(o => o.Api == (int)input.Api.Value);
        }

        var indices = queryable.OrderBy(o => o.Time).Take(MaxResultCount).ToList();

        return new ListResultDto<ApiKeyQueryBasicApiSnapshotDto>
        {
            Items =
                ObjectMapper.Map<List<ApiKeyQueryBasicApiSnapshotIndex>, List<ApiKeyQueryBasicApiSnapshotDto>>(indices)
        };
    }
}