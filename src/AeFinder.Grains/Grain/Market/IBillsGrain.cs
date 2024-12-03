using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IBillsGrain: IGrainWithStringKey
{
    Task<BillDto> CreateOrderLockBillAsync(CreateOrderLockBillDto dto);

    Task<BillDto> CreateSubscriptionLockBillAsync(CreateSubscriptionBillDto dto);
    Task<BillDto> GetBillByIdAsync(string billingId);
    Task<BillDto> CreateChargeBillAsync(string organizationId, string subscriptionId, string description,
        decimal chargeFee, decimal refundAmount);
    Task<BillDto> GetLatestLockedBillAsync(string subscriptionId);
    Task<BillDto> CreateRefundBillAsync(CreateRefundBillDto dto);
    Task<decimal> CalculateFirstMonthAmount(decimal monthlyFee);
    Task<decimal> CalculateChargeAmount(RenewalDto renewalInfo, decimal monthlyFee);
}