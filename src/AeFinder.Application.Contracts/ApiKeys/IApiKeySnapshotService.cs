using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.ApiKeys;

public interface IApiKeySnapshotService
{
    Task AddOrUpdateApiKeySummarySnapshotIndexAsync(ApiKeySummarySnapshotChangedEto input);
    Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotChangedEto input);
    Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotChangedEto input);
    Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotChangedEto input);
    Task<ListResultDto<ApiKeySummarySnapshotDto>> GetApiKeySummarySnapshotsAsync(Guid organizationId, GetSnapshotInput input);
    Task<ListResultDto<ApiKeySnapshotDto>> GetApiKeySnapshotsAsync(Guid organizationId, Guid? apiKeyId, GetSnapshotInput input);
    Task<ListResultDto<ApiKeyQueryAeIndexerSnapshotDto>> GetApiKeyQueryAeIndexerSnapshotsAsync(Guid organizationId, Guid apiKeyId, GetQueryAeIndexerSnapshotInput input);
    Task<ListResultDto<ApiKeyQueryBasicApiSnapshotDto>> GetApiKeyQueryBasicApiSnapshotsAsync(Guid organizationId, Guid apiKeyId, GetQueryBasicApiSnapshotInput input);
}