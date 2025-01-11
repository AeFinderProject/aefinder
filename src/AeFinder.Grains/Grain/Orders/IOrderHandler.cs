using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Orders;

public interface IOrderHandler
{
    Task HandleOrderCreatedAsync(OrderState orderState);
}