using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Market;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Market")]
[Route("api/market")]
public class MarketController : AeFinderController
{
    private readonly IProductService _productService;
    private readonly IRenewalService _renewalService;
    private readonly IBillService _billService;
    private readonly IOrderService _orderService;

    public MarketController(IProductService productService, IRenewalService renewalService, IBillService billService,
        IOrderService orderService)
    {
        _productService = productService;
        _renewalService = renewalService;
        _billService = billService;
        _orderService = orderService;
    }

    // GET
    [HttpGet]
    [Route("pod-resources/level")]
    [Authorize]
    public async Task<List<FullPodResourceDto>> GetFullPodResourceInfoAsync()
    {
        return await _productService.GetFullPodResourceInfoAsync();
    }

    [HttpGet]
    [Route("pod-resource/full")]
    [Authorize]
    public async Task<FullPodResourceDto> GetUserCurrentFullPodResourceAsync(string organizationId, string appId)
    {
        return await _renewalService.GetUserCurrentFullPodResourceAsync(organizationId, appId);
    }

    [HttpGet]
    [Route("api-query-count/free")]
    [Authorize]
    public async Task<int> GetUserApiQueryFreeCountAsync(string organizationId)
    {
        return await _renewalService.GetUserApiQueryFreeCountAsync(organizationId);
    }

    [HttpGet]
    [Route("api-query-count/monthly")]
    [Authorize]
    public async Task<long> GetUserMonthlyApiQueryAllowanceAsync(string organizationId)
    {
        return await _renewalService.GetUserMonthlyApiQueryAllowanceAsync(organizationId);
    }

    [HttpGet]
    [Route("api-query-count/regular")]
    [Authorize]
    public async Task<ApiQueryCountResourceDto> GetRegularApiQueryCountProductInfoAsync()
    {
        return await _productService.GetRegularApiQueryCountProductInfoAsync();
    }

    [HttpPost]
    [Route("calculate/resource-bill-plan")]
    [Authorize]
    public async Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input)
    {
        return await _billService.GetProductBillingPlanAsync(input);
    }

    [HttpPost]
    [Route("create/order")]
    [Authorize]
    public async Task<List<BillDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        return await _orderService.CreateOrderAsync(dto);
    }
    
    [HttpGet]
    [Route("transaction-history")]
    [Authorize]
    public async Task<PagedResultDto<TransactionHistoryDto>> GetOrganizationTransactionHistoryAsync(string organizationId)
    {
        return await _billService.GetOrganizationTransactionHistoryAsync(organizationId);
    }
    
    [HttpGet]
    [Route("invoices")]
    [Authorize]
    public async Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync(string organizationId)
    {
        return await _billService.GetInvoicesAsync(organizationId);
    }

    [HttpPost]
    [Route("cancel/order")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task CancelOrderAndBillAsync(string organizationId, string orderId, string billingId)
    {
        await _orderService.CancelOrderAndBillAsync(organizationId, orderId, billingId);
    }
}