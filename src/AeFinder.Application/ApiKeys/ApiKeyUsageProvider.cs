using System;
using System.Threading.Tasks;
using AeFinder.Billings;
using AeFinder.Merchandises;

namespace AeFinder.ApiKeys;

public class ApiKeyUsageProvider : IResourceUsageProvider
{
    public MerchandiseType MerchandiseType { get; } = MerchandiseType.ApiQuery;

    private readonly IApiKeyService _apiKeyService;

    public ApiKeyUsageProvider(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    public async Task<long> GetUsageAsync(Guid organizationId, DateTime dateTime)
    {
        return await _apiKeyService.GetMonthQueryCountAsync(organizationId, dateTime);
    }
}