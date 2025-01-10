using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Merchandises;
using AeFinder.Models;
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
    private readonly IAssetInitializationService _assetInitializationService;

    public AssetController(IOrganizationAppService organizationAppService, IAssetService assetService,
        IAssetInitializationService assetInitializationService)
    {
        _organizationAppService = organizationAppService;
        _assetService = assetService;
        _assetInitializationService = assetInitializationService;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<AssetDto>> GetListsAsync(GetAssetInput input)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _assetService.GetListAsync(orgId, input);
    }
    
    [HttpGet]
    [Route("all/{orgId}")]
    [Authorize]
    public async Task<PagedResultDto<AssetDto>> GetAllListsAsync(Guid orgId, GetAssetInput input)
    {
        return await _assetService.GetAllListAsync(orgId, input);
    }
    
    [HttpGet]
    [Route("{id}")]
    [Authorize]
    public async Task<AssetDto> GetAsync(Guid orgId)
    {
        return await _assetService.GetAsync(orgId);
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
    
    [HttpPost]
    [Route("initialization")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task InitializeAsync(InitializeAssetInput input)
    {
        var orgIds = input.OrganizationIds;
        if (orgIds.Count == 0)
        {
            orgIds = (await _organizationAppService.GetAllOrganizationUnitsAsync()).Select(o => o.Id).ToList();
        }

        foreach (var orgId in orgIds)
        {
            await _assetInitializationService.InitializeAsync(orgId);
        }
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}