using System.Threading.Tasks;
using AeFinder.User;
using AeFinder.User.Dto;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Identity;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Users")]
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
    
    [HttpPost("app")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task RegisterAppAuthentication(string appId,string deployKey)
    {
        await _userAppService.RegisterAppAuthentication(appId, deployKey);
    }
    
    [HttpGet("info")]
    [Authorize]
    public virtual async Task<IdentityUserExtensionDto> GetUserInfoAsync()
    {
        return await _userAppService.GetUserInfoAsync();
    }

    [HttpPost("reset/password")]
    [Authorize]
    public virtual async Task ResetPasswordAsync(ResetPasswordInput input)
    {
        await _userAppService.ResetPasswordAsync(input.UserName, input.NewPassword);
    }
    
    // [HttpGet("client/displayName")]
    // [Authorize]
    // public virtual async Task<string> GetClientDisplayNameAsync(string clientId)
    // {
    //     return await _userAppService.GetClientDisplayNameAsync(clientId);
    // }

    [HttpPost("bind/wallet")]
    [Authorize]
    public virtual async Task<IdentityUserExtensionDto> BindUserWalletAsync(BindUserWalletInput input)
    {
        return await _userAppService.BindUserWalletAsync(input);
    }

    [HttpGet("register/pending")]
    public virtual async Task<bool> IsRegisterPendingAsync(string email)
    {
        return await _userAppService.IsRegisterPendingAsync(email);
    }

    [HttpPost("register")]
    public virtual async Task RegisterAsync(RegisterUserInput input)
    {
        await _userAppService.RegisterAsync(input);
    }
    
    [HttpPost("register/confirm/{code}")]
    public virtual async Task RegisterAsync(string code)
    {
        await _userAppService.RegisterConfirmAsync(code);
    }
    
    [HttpPost("register/resend")]    
    public virtual async Task ResendRegisterEmailAsync(ResendEmailInput input)
    {
        await _userAppService.ResendRegisterEmailAsync(input);
    }
    
    [HttpGet("register/enable")]
    public virtual async Task<bool> RegisterEnableAsync()
    {
        await _userAppService.IsRegisterEnableAsyncAsync();
    }
}