using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Orders;

public interface IOrderValidationProvider
{
    Task<bool> ValidateBeforeOrderAsync(OrderState orderState);
}