using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.User;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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

    public async Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input)
    {
        var result = new BillingPlanDto();
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(input.ProductId);
        result.MonthlyUnitPrice = productInfo.MonthlyUnitPrice;
        var monthlyFee = result.MonthlyUnitPrice * input.ProductNum;
        result.BillingCycleMonthCount = input.PeriodMonths;
        result.PeriodicCost = result.BillingCycleMonthCount * monthlyFee;
        var organizationGrainId = await GetOrganizationGrainIdAsync(input.OrganizationId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        result.FirstMonthCost = await billsGrain.CalculateFirstMonthLockAmount(monthlyFee);
        return result;
    }

    public async Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync(string organizationId)
    {
        //TODO: Check organization id

        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var bills=await billsGrain.GetUserAllBillsAsync(CurrentUser.Id.ToString());
        var invoiceInfoList = new List<InvoiceInfoDto>();
        foreach (var billDto in bills)
        {
            var invoiceInfo = ObjectMapper.Map<BillDto, InvoiceInfoDto>(billDto);
            invoiceInfoList.Add(invoiceInfo);
            if (billDto.RefundAmount > 0)
            {
                var refundInvoiceInfo = ObjectMapper.Map<BillDto, InvoiceInfoDto>(billDto);
                refundInvoiceInfo.BillingAmount = billDto.RefundAmount;
                refundInvoiceInfo.BillingType = BillingType.Refund;
                invoiceInfoList.Add(refundInvoiceInfo);
            }
        }
        return new PagedResultDto<InvoiceInfoDto>
        {
            TotalCount = invoiceInfoList.Count,
            Items = invoiceInfoList
        };
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