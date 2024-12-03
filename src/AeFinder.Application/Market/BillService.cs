using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.User;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.Market;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BillService : ApplicationService, IBillService
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;

    public BillService(IClusterClient clusterClient, IOrganizationAppService organizationAppService)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
    }

    public async Task<BillingPlanDto> GetProductBillingPlanAsync(string productId, int productNum, int monthCount)
    {
        var result = new BillingPlanDto();
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(productId);
        result.MonthlyUnitPrice = productInfo.MonthlyUnitPrice;
        var monthlyFee = result.MonthlyUnitPrice * productNum;
        result.BillingCycleMonthCount = monthCount;
        result.PeriodicCost = result.BillingCycleMonthCount * monthlyFee;
        var organizationGrainId = await GetOrganizationGrainIdAsync();
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        result.FirstMonthCost = await billsGrain.CalculateFirstMonthAmount(monthlyFee);
        return result;
    }

    private async Task<string> GetOrganizationGrainIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id.ToString("N");
    }
    
    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }
}