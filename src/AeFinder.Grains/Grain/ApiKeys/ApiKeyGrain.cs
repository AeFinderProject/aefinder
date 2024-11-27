using AeFinder.Grains.State.ApiKeys;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.ApiKeys;

public class ApiKeyGrain : AeFinderGrain<ApiKeyState>, IApiKeyGrain
{
    private readonly IObjectMapper _objectMapper;

    public ApiKeyGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task CreateAsync(Guid id, Guid organizationId, string name)
    {

    }

    public Task RenameAsync(string newName)
    {
        throw new NotImplementedException();
    }

    public Task RegenerateKeyAsync()
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync()
    {
        throw new NotImplementedException();
    }

    public Task SetSpendingLimitAsync(bool isEnable, decimal limitUsdt)
    {
        throw new NotImplementedException();
    }

    public Task SetAuthorisedAeIndexersAsync(List<string> appIds)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAuthorisedAeIndexersAsync(List<string> appIds)
    {
        throw new NotImplementedException();
    }

    public Task SetAuthorisedDomainsAsync(List<string> domains)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAuthorisedDomainsAsync(List<string> domains)
    {
        throw new NotImplementedException();
    }

    public async Task RecordQueryCountAsync(long query, DateTime dateTime)
    {
        
    }

    public async Task<ApiKeyInfo> GetAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<ApiKeyState, ApiKeyInfo>(State);
    }
}