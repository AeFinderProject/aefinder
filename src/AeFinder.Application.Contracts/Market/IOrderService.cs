using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IOrderService
{
    Task<List<BillDto>> CreateOrderAsync(CreateOrderDto dto);
    Task CancelOrderAsync(string organizationId, string orderId);
}