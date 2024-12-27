using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Orders;

public interface IOrderService
{
    Task AddOrUpdateIndexAsync(OrderChangedEto eto);
    Task<PagedResultDto<OrderDto>> GetListsAsync(Guid organizationId, GetOrderListInput input);
    Task<OrderDto> CreateAsync(Guid organizationId, Guid userId, CreateOrderInput input);
    Task<OrderDto> CalculateCostAsync(CreateOrderInput input);
    Task UpdateOrderStatusAsync(Guid organizationId, Guid id, OrderStatus status);
    Task ConfirmPaymentAsync(Guid organizationId, Guid id, string transactionId, DateTime paymentTime);
}