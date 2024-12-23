using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public interface IBillService
{
    Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input);
    Task<PagedResultDto<TransactionHistoryDto>> GetOrganizationTransactionHistoryAsync(GetTransactionHistoryInput input);
    Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync();
    Task<List<PendingBillDto>> GetPendingBillsAsync();
    Task<ApiQueryBillingOverviewDto> GetApiQueryDailyAndMonthlyCostAverageAsync();
}