using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nito.AsyncEx;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("AppDeploy")]
[Route("api/app-deploy")]
public class AppDeployController : AeFinderController
{
    private readonly IAppDeployManager _appDeployManager;
    private readonly IAppService _appService;
    private readonly IAppDeployService _appDeployService;

    public AppDeployController(IAppDeployManager appDeployManager, IAppService appService,IAppDeployService appDeployService)
    {
        _appDeployManager = appDeployManager;
        _appService = appService;
        _appDeployService = appDeployService;
    }

    [HttpPost]
    [Route("deploy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<string> CreateNewAppAsync(CreateNewAppInput input)
    {
        return await _appDeployService.DeployNewAppAsync(input.AppId, input.Version, input.ImageName);
    }
    
    [HttpPost]
    [Route("batch-deploy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task CreateNewAppsAsync(CreateNewAppsInput input)
    {
        var tasks = input.AppIds.Select(async appId =>
        {
            var app = await _appService.GetIndexAsync(appId);

            if (app.Versions.PendingVersion != null)
            {
                // await _appDeployManager.CreateNewAppAsync(appId, app.Versions.PendingVersion, input.ImageName);
                await _appDeployService.DeployNewAppAsync(appId, app.Versions.PendingVersion, input.ImageName);
            }

            if (app.Versions.CurrentVersion != null)
            {
                // await _appDeployManager.CreateNewAppAsync(appId, app.Versions.CurrentVersion, input.ImageName);
                await _appDeployService.DeployNewAppAsync(appId, app.Versions.CurrentVersion, input.ImageName);
            }
        });

        await tasks.WhenAll();
    }

    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppAsync(AppVersionInput input)
    {
        // await _appDeployManager.DestroyAppAsync(input.AppId, input.Version);
        await _appDeployService.DestroyAppAsync(input.AppId, input.Version);
    }
    
    [HttpPost]
    [Route("batch-destroy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppsAsync(AppIdsInput input)
    {
        var tasks = input.AppIds.Select(async appId =>
        {
            var app = await _appService.GetIndexAsync(appId);

            if (app.Versions.PendingVersion != null)
            {
                // await _appDeployManager.DestroyAppAsync(appId, app.Versions.PendingVersion);
                await _appDeployService.DestroyAppAsync(appId, app.Versions.PendingVersion);
            }

            if (app.Versions.CurrentVersion != null)
            {
                // await _appDeployManager.DestroyAppAsync(appId, app.Versions.CurrentVersion);
                await _appDeployService.DestroyAppAsync(appId, app.Versions.CurrentVersion);
            }
        });

        await tasks.WhenAll();
    }

    [HttpPost]
    [Route("restart")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RestartAppAsync(AppVersionInput input)
    {
        // await _appDeployManager.RestartAppAsync(input.AppId, input.Version);
        await _appDeployService.RestartAppAsync(input.AppId, input.Version);
    }
    
    [HttpPost]
    [Route("batch-restart")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RestartAppsAsync(AppIdsInput input)
    {
        var tasks = input.AppIds.Select(async appId =>
        {
            var app = await _appService.GetIndexAsync(appId);

            if (app.Versions.PendingVersion != null)
            {
                // await _appDeployManager.RestartAppAsync(appId, app.Versions.PendingVersion);
                await _appDeployService.RestartAppAsync(appId, app.Versions.PendingVersion);
            }

            if (app.Versions.CurrentVersion != null)
            {
                // await _appDeployManager.RestartAppAsync(appId, app.Versions.CurrentVersion);
                await _appDeployService.RestartAppAsync(appId, app.Versions.CurrentVersion);
            }
        });

        await tasks.WhenAll();
    }

    [HttpPost]
    [Route("update-image")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UpdateAppDockerImageAsync(UpdateAppDockerImageInput input)
    {
        await _appDeployService.UpdateAppDockerImageAsync(input.AppId, input.Version, input.ImageName);
    }
}