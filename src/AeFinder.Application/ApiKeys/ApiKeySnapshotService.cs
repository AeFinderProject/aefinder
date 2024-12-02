using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.ApiKeys;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ApiKeySnapshotService: AeFinderAppService, IApiKeySnapshotService
{
    public Task AddOrUpdateApiKeyQueryAeIndexerSnapshotIndexAsync(ApiKeyQueryAeIndexerSnapshotEto input)
    {
        throw new System.NotImplementedException();
    }

    public Task AddOrUpdateApiKeyQueryBasicApiSnapshotIndexAsync(ApiKeyQueryBasicApiSnapshotEto input)
    {
        throw new System.NotImplementedException();
    }

    public Task AddOrUpdateApiKeySnapshotIndexAsync(ApiKeySnapshotEto input)
    {
        throw new System.NotImplementedException();
    }

    public Task AddOrUpdateApiKeySummarySnapshotAsync(ApiKeySummarySnapshotEto input)
    {
        throw new System.NotImplementedException();
    }
}