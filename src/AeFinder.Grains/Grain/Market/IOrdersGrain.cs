using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IOrdersGrain: IGrainWithStringKey
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
    Task<OrderDto> GetOrderByIdAsync(string id);
    Task CancelOrderByIdAsync(string id);
    Task<OrderDto> GetLatestApiQueryCountOrderAsync(string organizationId, string userId);
    Task<OrderDto> GetLatestPodResourceOrderAsync(string organizationId, string userId, string appId);
}