using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IBillService
{
    Task<BillingPlanDto> GetFullPodResourceBillingPlanAsync(string productId);
    Task<BillingPlanDto> GetApiQueryCountBillingPlanAsync(string productId, int monthCount);
}