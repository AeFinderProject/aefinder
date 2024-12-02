using System;
using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiKeySnapshotService
{
    Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotChangedEto input);
    Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotChangedEto input);
    Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotChangedEto input);
    Task AddOrUpdateApiKeySummarySnapshotIndexAsync(ApiKeySummarySnapshotChangedEto input);
}