using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Models;
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
    public async Task<string> CreateNewAppAsync(CreateNewAppDto dto)
    {
        return await _appDeployManager.CreateNewAppAsync(dto.AppId, dto.Version, dto.ImageName);
    }

    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppAsync(DestroyAppDto dto)
    {
        await _appDeployManager.DestroyAppAsync(dto.AppId, dto.Version);
    }

    [HttpPost]
    [Route("restart")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RestartAppAsync(RestartAppDto dto)
    {
        await _appDeployManager.RestartAppAsync(dto.AppId, dto.Version);
    }
}