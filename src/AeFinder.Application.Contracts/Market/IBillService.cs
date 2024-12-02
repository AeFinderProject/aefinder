using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IBillService
{
    Task<BillingPlanDto> GetProductBillingPlanAsync(string productId, int productNum, int monthCount);
}