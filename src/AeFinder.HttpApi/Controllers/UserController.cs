using System.Threading.Tasks;
using AeFinder.User;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Identity;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("OrganizationUnits")]
[Route("api/users")]
public class UserController : AeFinderController
{
    private readonly IUserAppService _userAppService;
    public UserController(IUserAppService userAppService)
    {
        _userAppService = userAppService;
    }
    
    [HttpPost]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input)
    {
        return await _userAppService.RegisterUserWithOrganization(input);
    }
    
    // [HttpPost("app")]
    // [Authorize(Policy = "OnlyAdminAccess")]
    // public virtual async Task RegisterAppAuthentication(string appId,string deployKey)
    // {
    //     await _userAppService.RegisterAppAuthentication(appId, deployKey);
    // }
    
    [HttpGet("info")]
    [Authorize]
    public virtual async Task<IdentityUserDto> GetUserInfoAsync()
    {
        return await _userAppService.GetUserInfoAsync();
    }
}