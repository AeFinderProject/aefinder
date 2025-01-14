using System.Threading.Tasks;

namespace AeFinder.Billings;

public interface IBillingPaymentService
{
    Task RepayFailedBillingAsync(string organizationId, string billingId);
}