using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Orders;

public interface IOrderService
{
    Task AddOrUpdateIndexAsync(OrderChangedEto eto);
    Task UpdateIndexAsync(Guid id);
    Task<PagedResultDto<OrderDto>> GetListAsync(Guid organizationId, GetOrderListInput input);
    Task<OrderDto> GetAsync(Guid organizationId, Guid id);
    Task<OrderDto> CreateAsync(Guid organizationId, Guid userId, CreateOrderInput input);
    Task<OrderDto> CalculateCostAsync(CreateOrderInput input);
    Task PayAsync(Guid organizationId, Guid id, PayInput input);
    Task ConfirmPaymentAsync(Guid organizationId, Guid id, string transactionId, DateTime paymentTime);
    Task PaymentFailedAsync(Guid organizationId, Guid id);
    Task CancelAsync(Guid organizationId, Guid id);
}