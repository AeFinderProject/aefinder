using System.Threading.Tasks;
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
    [HttpPost]
    [Authorize]
    public virtual async Task<CreateOrganizationUnitDto> CreateOrganizationUnitAsync(CreateOrganizationUnitInput input)
    {
        // return _studioService.ApplyAeFinderAppNameAsync(input);
        return new CreateOrganizationUnitDto();
    }
}