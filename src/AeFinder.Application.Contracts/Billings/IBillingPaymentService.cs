using System.Threading.Tasks;

namespace AeFinder.Billings;

public interface IBillingPaymentService
{
    Task<string> GetTreasurerAsync();
}