using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerSnapshotGrain : AeFinderGrain<ApiKeyQueryAeIndexerSnapshotState>,
    IApiKeyQueryAeIndexerSnapshotGrain
{
    public async Task IncreaseQueryCountAsync(Guid appKeyId, string appId, long query,)
    {
        await ReadStateAsync();

        State.ApiKeyId = appKeyId;
        State.AppId = appId;
        State.TotalQuery += query;
        State.LastQueryTime = lastQueryTime;

        await WriteStateAsync();
        
        
    }
}