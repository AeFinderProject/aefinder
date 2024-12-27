using System;
using System.Threading.Tasks;
using AeFinder.Billings;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Orders;

public interface IOrderService
{
    Task<PagedResultDto<OrderDto>> GetListsAsync(Guid organizationId, GetOrderListInput input);
    Task<OrderDto> CreateAsync(Guid organizationId, Guid userId, CreateOrderInput input);
    Task<OrderDto> CalculateCostAsync(CreateOrderInput input);
    Task PayAsync(Guid organizationId, Guid id);
    Task CancelAsync(Guid organizationId, Guid id);
    Task ConfirmPaymentAsync(Guid orderId, string transactionId, DateTime paymentTime);
}