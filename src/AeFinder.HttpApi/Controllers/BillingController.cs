using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Billings;
using AeFinder.Merchandises;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Billing")]
[Route("api/billings")]
public class BillingController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillingService _billingService;

    public BillingController(IOrganizationAppService organizationAppService, IBillingService billingService)
    {
        _organizationAppService = organizationAppService;
        _billingService = billingService;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<BillingDto>> GetListAsync(GetBillingInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _billingService.GetListAsync(orgId, input);
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<BillingDto> GetAsync(Guid id)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _billingService.GetAsync(orgId, id);
    }
    
    [HttpPost]
    [Route("{id}/index")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UpdateIndexAsync(Guid id)
    {
        await _billingService.UpdateIndexAsync(id);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}