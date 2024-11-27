using AeFinder.Grains.State.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyQueryAeIndexerGrain : AeFinderGrain<ApiKeyQueryAeIndexerState>, IApiKeyQueryAeIndexerGrain
{
    public async Task IncreaseQueryCountAsync(Guid organizationId, Guid appKeyId, string appId, long query,
        DateTime dateTime)
    {
        await ReadStateAsync();

        State.OrganizationId = organizationId;
        State.ApiKeyId = appKeyId;
        State.AppId = appId;
        State.TotalQuery += query;
        State.LastQueryTime = dateTime;

        await WriteStateAsync();

        
    }
}