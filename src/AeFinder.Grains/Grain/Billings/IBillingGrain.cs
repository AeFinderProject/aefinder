using AeFinder.Grains.State.Billings;

namespace AeFinder.Grains.Grain.Billings;

public interface IBillingGrain : IGrainWithGuidKey
{
    Task<BillingState> GetAsync();
    Task CreateAsync(BillingState billing);
    Task PayAsync(string transactionId, DateTime paymentTime);
    Task ConfirmPaymentAsync();
    Task PaymentFailedAsync();
}