using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.AppResources;
using AeFinder.AppResources.Dto;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("AppResource")]
[Route("api/apps/resource-usages")]
public class AppResourceUsageController : AeFinderController
{
    private readonly IAppResourceUsageService _appResourceUsageService;
    private readonly IOrganizationAppService _organizationAppService;

    public AppResourceUsageController(IAppResourceUsageService appResourceUsageService,
        IOrganizationAppService organizationAppService)
    {
        _appResourceUsageService = appResourceUsageService;
        _organizationAppService = organizationAppService;
    }

    [HttpGet]
    [Route("{appId}")]
    [Authorize]
    public async Task<AppResourceUsageDto> GetAsync(string appId)
    {
        Guid? orgId = null;
        if (!CurrentUser.IsInRole(AeFinderApplicationConsts.AdminRoleName))
        {
            orgId = await GetOrganizationIdAsync();
        }

        return await _appResourceUsageService.GetAsync(orgId, appId);
    }
    
    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<AppResourceUsageDto>> GetListAsync(GetAppResourceUsageInput input)
    {
        Guid? orgId = null;
        if (!CurrentUser.IsInRole(AeFinderApplicationConsts.AdminRoleName))
        {
            orgId = await GetOrganizationIdAsync();
        }

        return await _appResourceUsageService.GetListAsync(orgId, input);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }
}