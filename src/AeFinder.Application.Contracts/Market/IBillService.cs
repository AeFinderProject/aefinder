using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public interface IBillService
{
    Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input);
    Task<PagedResultDto<TransactionHistoryDto>> GetOrganizationTransactionHistoryAsync(string organizationId);
    Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync(string organizationId);
}