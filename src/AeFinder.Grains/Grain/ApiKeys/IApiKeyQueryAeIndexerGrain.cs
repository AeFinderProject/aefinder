using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryAeIndexerGrain : IGrainWithStringKey
{
    Task<ApiKeyQueryAeIndexerInfo> GetAsync();
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime);
}