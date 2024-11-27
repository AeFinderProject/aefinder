namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyGrain : IGrainWithGuidKey
{
    Task CreateAsync(Guid id, Guid organizationId, string name);
    Task RenameAsync(string newName);
    Task RegenerateKeyAsync();
    Task DeleteAsync();
    Task SetSpendingLimitAsync(bool isEnable, decimal limitUsdt);
    Task SetAuthorisedAeIndexersAsync(List<string> appIds);
    Task DeleteAuthorisedAeIndexersAsync(List<string> appIds);
    Task SetAuthorisedDomainsAsync(List<string> domains);
    Task DeleteAuthorisedDomainsAsync(List<string> domains);
    Task RecordQueryCountAsync(long query, DateTime dateTime);
    Task<ApiKeyInfo> GetAsync();
}