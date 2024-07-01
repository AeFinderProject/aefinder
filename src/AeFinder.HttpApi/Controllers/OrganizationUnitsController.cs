using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.User;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Identity;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("OrganizationUnits")]
[Route("api/organizationUnits")]
public class OrganizationUnitsController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppService _appService;

    public OrganizationUnitsController(IOrganizationAppService organizationAppService, IAppService appService)
    {
        _organizationAppService = organizationAppService;
        _appService = appService;
    }

    [HttpPost]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task<OrganizationUnitDto> CreateOrganizationUnitAsync(CreateOrganizationUnitInput input)
    {
        return await _organizationAppService.CreateOrganizationUnitAsync(input.DisplayName);
    }

    [HttpGet]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync()
    {
        return await _organizationAppService.GetAllOrganizationUnitsAsync();
    }
    
    [HttpGet("user")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync(string userId)
    {
        Guid userGuid;
        if (!Guid.TryParse(userId, out userGuid))
        {
            throw new UserFriendlyException("Invalid userId string");
        }
        return await _organizationAppService.GetOrganizationUnitsByUserIdAsync(userGuid);
    }
    
    [HttpGet("users")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task<List<IdentityUserDto>> GetUsersInOrganizationUnitAsync(string organizationUnitId)
    {
        Guid organizationUnitGuid;
        if (!Guid.TryParse(organizationUnitId, out organizationUnitGuid))
        {
            throw new UserFriendlyException("Invalid OrganizationUnitId string");
        }
        return await _organizationAppService.GetUsersInOrganizationUnitAsync(organizationUnitGuid);
    }
        
    [HttpPut]
    [Route("{id}/max-app-count")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task SetMaxAppCountAsync(Guid id, int maxAppCount)
    {
        await _appService.SetMaxAppCountAsync(id, maxAppCount);
    }
    
    [HttpGet]
    [Route("{id}/max-app-count")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<int> GetMaxAppCountAsync(Guid id)
    {
        return await _appService.GetMaxAppCountAsync(id);
    }
}