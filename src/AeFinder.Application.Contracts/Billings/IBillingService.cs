using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Billings;

public interface IBillingService
{
    Task<BillingDto> GetAsync(Guid id);
    Task<PagedResultDto<BillingDto>> GetListsAsync(Guid organizationId, GetBillingInput input);
    Task<BillingDto> CreateAsync(Guid organizationId, BillingType type, DateTime dateTime);
    Task PayAsync(Guid id, string transactionId, DateTime paymentTime);
    Task ConfirmPaymentAsync(Guid id);
}