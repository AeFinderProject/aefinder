using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Enums;
using AeFinder.Merchandises;
using AeFinder.Orders;
using AeFinder.User;
using AeFinder.User.Dto;
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
        var organization = await GetOrganizationAsync();
        return await _orderService.GetListAsync(organization.Id, input);
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<OrderDto> GetAsync(Guid id)
    {
        var organization = await GetOrganizationAsync();
        return await _orderService.GetAsync(organization.Id, id);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<OrderDto> CreateAsync(CreateOrderInput input)
    {
        var organization = await GetOrganizationAsync();
        await CheckOrganizationAsync(organization);
        return await _orderService.CreateAsync(organization.Id, CurrentUser.Id.Value, input);
    }
    
    [HttpPost]
    [Route("{id}/pay")]
    [Authorize]
    public async Task PayAsync(Guid id, PayInput input)
    {
        var organization = await GetOrganizationAsync();
        await CheckOrganizationAsync(organization);
        await _orderService.PayAsync(organization.Id, id, input);
    }
    
    [HttpPost]
    [Route("{id}/cancel")]
    [Authorize]
    public async Task CancelAsync(Guid id)
    {
        var organization = await GetOrganizationAsync();
        await CheckOrganizationAsync(organization);
        await _orderService.CancelAsync(organization.Id, id);
    }
    
    [HttpPost]
    [Route("cost")]
    [Authorize]
    public async Task<OrderDto> CalculateCostAsync(CreateOrderInput input)
    {
        return await _orderService.CalculateCostAsync(input);
    }
    
    [HttpPost]
    [Route("{id}/index")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UpdateIndexAsync(Guid id)
    {
        await _orderService.UpdateIndexAsync(id);
    }

    private async Task CheckOrganizationAsync(OrganizationUnitDto organization)
    {
        if (organization.OrganizationStatus != OrganizationStatus.Normal)
        {
            throw new UserFriendlyException("Organization status is abnormal");
        }
    }

    private async Task<OrganizationUnitDto> GetOrganizationAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First();
    }
}