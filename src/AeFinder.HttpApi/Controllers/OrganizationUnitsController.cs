using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.User;
using AeFinder.User.Dto;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("OrganizationUnits")]
[Route("api/organizations")]
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
    public virtual async Task<PagedResultDto<OrganizationIndexDto>> GetAllOrganizationUnitsAsync(GetOrganizationListInput input)
    {
        return await _organizationAppService.GetOrganizationListAsync(input);
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
    
    [HttpGet("user/all")]
    [Authorize]
    public virtual async Task<List<OrganizationUnitDto>> GetUserOrganizationUnitsAsync()
    {
        return await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
    }
    
    [HttpGet("balance")]
    [Authorize]
    public virtual async Task<OrganizationBalanceDto> GetOrganizationBalanceAsync()
    {
        return await _organizationAppService.GetOrganizationBalanceAsync();
    }
}