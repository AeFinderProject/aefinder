using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IOrdersGrain: IGrainWithStringKey
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
    Task<OrderDto> GetOrderByIdAsync(string id);
    Task CancelOrderByIdAsync(string id);
}