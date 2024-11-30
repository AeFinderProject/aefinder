using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IBillsGrain: IGrainWithStringKey
{
    Task<BillDto> CreateOrderLockBillAsync(CreateOrderBillDto dto);

    Task<BillDto> CreateSubscriptionLockBillAsync(CreateSubscriptionBillDto dto);
    Task<BillDto> GetBillByIdAsync(string billingId);
    Task<BillDto> CreateChargeBillAsync(string organizationId, string subscriptionId, string description,
        decimal chargeFee);
    Task<BillDto> GetLatestLockedBillAsync(string subscriptionId);
    Task CreateRefundBillAsync(CreateRefundBillDto dto);
}