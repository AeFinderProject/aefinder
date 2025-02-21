using AeFinder.Grains.State.Billings;

namespace AeFinder.Grains.Grain.Billings;

public interface IMonthlyBillingGrain : IGrainWithStringKey
{
    Task<MonthlyBillingState> GetAsync();
    Task CreateMonthlyBillingAsync(Guid organizationId, DateTime dateTime, Guid settlementBillingId);
    Task SetAdvancePaymentBillingAsync(Guid billingId);
}