using System;
using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiKeySnapshotService
{
    Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotEto input);
    Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotEto input);
    Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotEto input);
    Task AddOrUpdateApiKeySummarySnapshotAsync(ApiKeySummarySnapshotEto input);
}