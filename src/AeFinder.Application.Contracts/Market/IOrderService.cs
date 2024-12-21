using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IOrderService
{
    Task<List<BillDto>> CreateOrderAsync(CreateOrderDto dto);
    Task UpdateBillToPendingStatusAsync(string billingId);
    Task CancelBillAsync(string billingId);
    Task CancelOrderAndBillAsync(string organizationId, string orderId, string billingId);
    Task OrderFreeApiQueryCountAsync(string organizationId);
}