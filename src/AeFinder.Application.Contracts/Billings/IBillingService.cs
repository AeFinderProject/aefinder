using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Billings;

public interface IBillingService
{
    Task AddOrUpdateIndexAsync(BillingChangedEto eto);
    Task UpdateIndexAsync(Guid id);
    Task<BillingDto> GetAsync(Guid? organizationId,Guid id);
    Task<PagedResultDto<BillingDto>> GetListAsync(Guid? organizationId, GetBillingInput input);
    Task<BillingDto> CreateAsync(Guid organizationId, BillingType type, DateTime dateTime);
    Task PayAsync(Guid id, string transactionId, DateTime paymentTime);
    Task ConfirmPaymentAsync(Guid id);
    Task PaymentFailedAsync(Guid id);
}