using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IRenewalGrain: IGrainWithStringKey
{
    Task<RenewalDto> CreateAsync(CreateRenewalDto dto);
    Task<RenewalDto> GetRenewalSubscriptionInfoByIdAsync(string subscriptionId);
    Task UpdateRenewalDateToNextPeriodAsync(string subscriptionId);
    Task UpdateLastChargeDateAsync(string subscriptionId, DateTime lastChargeDate);
    Task<RenewalDto> GetApiQueryCountRenewalInfoAsync(string organizationId,
        string productId);

    Task<RenewalDto> GetPodResourceRenewalInfoAsync(string organizationId, string appId,
        string productId);

    Task CancelRenewalByIdAsync(string subscriptionId);
    Task<long> GetOrganizationMonthlyApiQueryAllowanceAsync(string organizationId);
    Task<string> GetCurrentSubscriptionIdAsync(string orderId);
    Task<bool> CheckRenewalInfoIsExistAsync(string organizationId, string productId);
    Task<RenewalDto> GetCurrentPodResourceRenewalInfoAsync(string organizationId, string appId);
    Task<List<RenewalDto>> GetAllActiveRenewalInfosAsync(string organizationId);
    Task<RenewalDto> GetRenewalInfoByOrderIdAsync(string orderId);
    Task<RenewalDto> GetRenewalInfoByProductTypeAsync(ProductType productType, string appId);
    Task<List<RenewalDto>> GetAllActiveRenewalInfoListAsync();
}