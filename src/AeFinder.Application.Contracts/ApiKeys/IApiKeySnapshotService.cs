using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.ApiKeys;

public interface IApiKeySnapshotService
{
    Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotChangedEto input);
    Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotChangedEto input);
    Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotChangedEto input);
    Task AddOrUpdateApiKeySummarySnapshotIndexAsync(ApiKeySummarySnapshotChangedEto input);
    Task<ListResultDto<ApiKeySummarySnapshotDto>> GetApiKeySummarySnapshotsAsync(Guid organizationId, GetSnapshotInput input);
    Task<ListResultDto<ApiKeySnapshotDto>> GetApiKeySnapshotsAsync(Guid organizationId, GetSnapshotInput input);
    Task<ListResultDto<ApiKeyQueryAeIndexerSnapshotDto>> GetApiKeyQueryAeIndexerSnapshotsAsync(Guid organizationId, GetQueryAeIndexerSnapshotInput input);
    Task<ListResultDto<ApiKeyQueryBasicApiSnapshotDto>> GetApiKeyQueryBasicApiSnapshotsAsync(Guid organizationId, GetQueryBasicApiSnapshotInput input);
}