using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
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
[ControllerName("Asset")]
[Route("api/assets")]
public class AssetController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IClock _clock;

    public AssetController(IOrganizationAppService organizationAppService, IClock clock)
    {
        _organizationAppService = organizationAppService;
        _clock = clock;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<AssetDto>> GetListsAsync(GetAssetInput input)
    {
        throw new NotImplementedException();
    }
    
    [HttpGet]
    [Route("monthly-cost")]
    [Authorize]
    public async Task<decimal> GetMonthlyCostAsync(DateTime dateTime)
    {
        throw new NotImplementedException();
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}