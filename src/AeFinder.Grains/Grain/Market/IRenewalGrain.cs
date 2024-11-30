using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IRenewalGrain: IGrainWithStringKey
{
    Task<RenewalDto> CreateAsync(CreateRenewalDto dto);
    Task<RenewalDto> GetRenewalSubscriptionInfoByIdAsync(string subscriptionId);
    Task UpdateRenewalDateToNextPeriodAsync(string subscriptionId);

    Task<RenewalDto> GetApiQueryCountRenewalInfoAsync(string organizationId, string userId,
        string productId);

    Task<RenewalDto> GetPodResourceRenewalInfoAsync(string organizationId, string userId, string appId,
        string productId);

    Task CancelRenewalByIdAsync(string subscriptionId);
}