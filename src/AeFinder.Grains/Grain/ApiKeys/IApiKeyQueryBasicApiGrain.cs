using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryBasicApiGrain : IGrainWithStringKey
{
    Task<ApiKeyQueryBasicApiInfo> GetAsync();
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicApi api, long query,
        DateTime dateTime);
}