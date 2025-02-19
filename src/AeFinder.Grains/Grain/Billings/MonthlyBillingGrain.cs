using AeFinder.Grains.State.Billings;

namespace AeFinder.Grains.Grain.Billings;

public class MonthlyBillingGrain : AeFinderGrain<MonthlyBillingState>, IMonthlyBillingGrain
{
    public async Task<MonthlyBillingState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }

    public async Task CreateMonthlyBillingAsync(Guid organizationId, DateTime dateTime, Guid settlementBillingId)
    {
        State.OrganizationId = organizationId;
        State.BillingDate = dateTime.ToMonthDate();
        State.SettlementBillingId = settlementBillingId;
        await WriteStateAsync();
    }
    
    public async Task SetAdvancePaymentBillingAsync(Guid billingId)
    {
        await ReadStateAsync();
        State.AdvancePaymentBillingId = billingId;
        await WriteStateAsync();
    }
}