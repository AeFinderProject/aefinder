using AeFinder.Grains.State.Orders;
using AeFinder.Orders;

namespace AeFinder.Grains.Grain.Orders;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task<OrderState> CreateAsync(Guid id, Guid organizationId, Guid userId, CreateOrderInput input);
    Task<OrderState> GetAsync();
    Task PayAsync(PaymentType paymentType);
    Task ConfirmPaymentAsync(string transactionId, DateTime paymentTime);
    Task PaymentFailedAsync();
    Task CancelAsync();
}