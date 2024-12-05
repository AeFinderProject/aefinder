using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyGrain : IGrainWithGuidKey
{
    Task<ApiKeyInfo> CreateAsync(Guid id, Guid organizationId, CreateApiKeyInput input);
    Task<ApiKeyInfo> UpdateAsync(UpdateApiKeyInput input);
    Task<string> RegenerateKeyAsync();
    Task DeleteAsync();
    Task SetAuthorisedAeIndexersAsync(List<string> appIds);
    Task DeleteAuthorisedAeIndexersAsync(List<string> appIds);
    Task SetAuthorisedDomainsAsync(List<string> domains);
    Task DeleteAuthorisedDomainsAsync(List<string> domains);
    Task SetAuthorisedApisAsync(Dictionary<BasicApi, bool> apis);
    Task RecordQueryCountAsync(long query, DateTime dateTime);
    Task<ApiKeyInfo> GetAsync();
    Task<long?> GetAvailabilityQueryAsync(DateTime dateTime);
}