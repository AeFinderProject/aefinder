using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("AppDeploy")]
[Route("api/app-deploy")]
public class AppDeployController : AeFinderController
{
    private readonly IAppDeployManager _appDeployManager;

    public AppDeployController(IAppDeployManager appDeployManager)
    {
        _appDeployManager = appDeployManager;
    }
    
    [HttpPost]
    [Route("deploy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<string> CreateNewAppAsync(CreateNewAppInput input)
    {
        return await _appDeployManager.CreateNewAppAsync(input.AppId, input.Version, input.ImageName);
    }

    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppAsync(AppVersionInput input)
    {
        await _appDeployManager.DestroyAppAsync(input.AppId, input.Version);
    }

    [HttpPost]
    [Route("restart")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RestartAppAsync(AppVersionInput input)
    {
        await _appDeployManager.RestartAppAsync(input.AppId, input.Version);
    }
}