using AeFinder.Orders;

namespace AeFinder.Grains.Grain.Orders;

public interface IOrderCostProvider
{
    Task<OrderCost> CalculateCostAsync(List<CreateOrderDetail> details, DateTime orderTime, DateTime endTime);
}