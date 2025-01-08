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
    private readonly IAssetService _assetService;

    public AssetController(IOrganizationAppService organizationAppService, IAssetService assetService)
    {
        _organizationAppService = organizationAppService;
        _assetService = assetService;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<AssetDto>> GetListsAsync(GetAssetInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _assetService.GetListAsync(orgId, input);
    }
    
    [HttpPost]
    [Route("relate")]
    [Authorize]
    public async Task RelateAppAsync(RelateAppInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        await _assetService.RelateAppAsync(orgId, input);
    }
    
    [HttpPost]
    [Route("{id}/index")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UpdateIndexAsync(Guid id)
    {
        await _assetService.UpdateIndexAsync(id);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}