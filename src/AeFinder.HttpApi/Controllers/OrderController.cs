using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Merchandises;
using AeFinder.Orders;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Order")]
[Route("api/orders")]
public class OrderController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IClock _clock;
    private readonly IOrderService _orderService;

    public OrderController(IOrganizationAppService organizationAppService, IClock clock, IOrderService orderService)
    {
        _organizationAppService = organizationAppService;
        _clock = clock;
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<OrderDto>> GetListAsync(GetOrderListInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _orderService.GetListAsync(orgId, input);
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<OrderDto> GetAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _orderService.GetAsync(orgId, id);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<OrderDto> CreateAsync(CreateOrderInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _orderService.CreateAsync(orgId, CurrentUser.Id.Value, input);
    }
    
    [HttpPost]
    [Route("{id}/pay")]
    [Authorize]
    public async Task PayAsync(Guid id, PayInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _orderService.PayAsync(orgId, id, input);
    }
    
    [HttpPost]
    [Route("{id}/cancel")]
    [Authorize]
    public async Task CancelAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        await _orderService.CancelAsync(orgId, id);
    }
    
    [HttpPost]
    [Route("cost")]
    [Authorize]
    public async Task<OrderDto> CalculateCostAsync(CreateOrderInput input)
    {
        return await _orderService.CalculateCostAsync(input);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}