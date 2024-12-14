using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public interface IBillService
{
    Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input);
    Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync(string organizationId);
}