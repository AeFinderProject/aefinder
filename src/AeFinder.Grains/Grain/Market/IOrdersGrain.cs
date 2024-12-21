using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IOrdersGrain: IGrainWithStringKey
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task UpdateOrderToPendingStatusAsync(string orderId);
    Task<OrderDto> GetOrderByIdAsync(string id);
    Task CancelOrderByIdAsync(string id);
    Task<OrderDto> GetLatestApiQueryCountOrderAsync(string organizationId);
    Task<OrderDto> GetLatestPodResourceOrderAsync(string organizationId, string appId);
    Task UpdateOrderStatusAsync(string orderId, OrderStatus orderStatus);
}