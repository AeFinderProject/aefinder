using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("OrganizationUnits")]
[Route("api/organizationUnits")]
public class OrganizationUnitsController : AeFinderController
{
    private readonly IOrganizationAppService _organizationAppService;
    public OrganizationUnitsController(IOrganizationAppService organizationAppService)
    {
        _organizationAppService = organizationAppService;
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
    
    
}