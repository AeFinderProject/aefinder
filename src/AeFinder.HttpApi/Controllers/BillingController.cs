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

    public BillingController(IOrganizationAppService organizationAppService)
    {
        _organizationAppService = organizationAppService;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<BillingDto>> GetListsAsync(GetBillingInput input)
    {
        throw new NotImplementedException();
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<BillingDto> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}