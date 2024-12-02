using System;
using System.Threading.Tasks;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.ApiKeys;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiKeySnapshotService: AeFinderAppService, IApiKeySnapshotService
{
    private readonly IEntityMappingRepository<ApiKeyQueryAeIndexerSnapshotIndex, string> _apiKeyQueryAeIndexerSnapshotIndexRepository;
    private readonly IEntityMappingRepository<ApiKeyQueryBasicApiSnapshotIndex, string> _apiKeyQueryBasicApiIndexRepository;
    private readonly IEntityMappingRepository<ApiKeySnapshotIndex, string> _apiKeySnapshotIndexRepository;
    private readonly IEntityMappingRepository<ApiKeySummarySnapshotIndex, string> _apiKeySummarySnapshotIndexRepository;
    
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
}