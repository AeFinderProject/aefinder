using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyGrain : IGrainWithGuidKey
{
    Task<ApiKeyInfo> CreateAsync(Guid id, Guid organizationId, string name);
    Task RenameAsync(string newName);
    Task<string> RegenerateKeyAsync();
    Task DeleteAsync();
    Task SetSpendingLimitAsync(bool isEnable, decimal limitUsdt);
    Task SetAuthorisedAeIndexersAsync(List<string> appIds);
    Task DeleteAuthorisedAeIndexersAsync(List<string> appIds);
    Task SetAuthorisedDomainsAsync(List<string> domains);
    Task DeleteAuthorisedDomainsAsync(List<string> domains);
    Task SetAuthorisedApisAsync(Dictionary<BasicDataApiType, bool> apis);
    Task RecordQueryCountAsync(long query, DateTime dateTime);
    Task<ApiKeyInfo> GetAsync();
    Task<long?> GetAvailabilityQueryAsync(DateTime dateTime);
}