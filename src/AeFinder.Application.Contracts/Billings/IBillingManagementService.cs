using System;
using System.Threading.Tasks;

namespace AeFinder.Billings;

public interface IBillingManagementService
{
    Task GenerateMonthlyBillingAsync(Guid organizationId, DateTime month);
    Task PayAsync(Guid billingId);
    Task<bool> IsPaymentFailedAsync(Guid organizationId, DateTime month);
    Task RePayAsync(Guid billingId);
}