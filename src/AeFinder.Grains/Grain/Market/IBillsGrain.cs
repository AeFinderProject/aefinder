using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IBillsGrain: IGrainWithStringKey
{
    Task<BillDto> CreateOrderLockBillAsync(CreateOrderLockBillDto dto);
    Task<BillDto> CreateSubscriptionLockBillAsync(CreateSubscriptionBillDto dto);
    Task<BillDto> GetBillByIdAsync(string billingId);
    Task<BillDto> CreateChargeBillAsync(CreateChargeBillDto dto);
    Task<BillDto> GetLatestLockedBillAsync(string subscriptionId);
    Task<decimal> CalculateFirstMonthLockAmount(decimal monthlyFee);
    Task<decimal> CalculatePodResourceMidWayChargeAmountAsync(RenewalDto renewalInfo, decimal monthlyFee,
        DateTime? podResourceStartUseDay);
    Task<decimal> CalculateApiQueryMonthlyChargeAmountAsync(long monthlyQueryCount);
    Task<BillDto> UpdateBillingTransactionInfoAsync(string billingId, string transactionId,
        decimal transactionAmount, string walletAddress);
    Task<BillDto> GetPendingChargeBillByOrderIdAsync(string orderId);
    Task<List<BillDto>> GetOrganizationAllBillsAsync(string organizationId);
    Task<List<BillDto>> GetAllPendingBillAsync();
}