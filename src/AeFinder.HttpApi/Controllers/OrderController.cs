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

    public OrderController(IOrganizationAppService organizationAppService, IClock clock)
    {
        _organizationAppService = organizationAppService;
        _clock = clock;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<OrderDto>> GetListsAsync(GetOrderListInput input)
    {
        throw new NotImplementedException();
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<ListResultDto<OrderDto>> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost]
    [Authorize]
    public async Task<OrderDto> CreateAsync(CreateOrderInput input)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost]
    [Route("{id}/pay")]
    [Authorize]
    public async Task PayAsync(string id, PayInput input)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost]
    [Route("{id}/cancel")]
    [Authorize]
    public async Task CancelAsync(string id)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost]
    [Route("cost")]
    [Authorize]
    public async Task<OrderDto> CalculateCostAsync(CreateOrderInput input)
    {
        throw new NotImplementedException();
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}