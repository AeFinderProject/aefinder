using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
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

            if (!app.Versions.PendingVersion.IsNullOrEmpty())
            {
                await _appDeployService.DeployNewAppAsync(appId, app.Versions.PendingVersion, input.ImageName);
            }

            if (!app.Versions.CurrentVersion.IsNullOrEmpty())
            {
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

            if (!app.Versions.PendingVersion.IsNullOrEmpty())
            {
                await _appDeployService.DestroyAppAsync(appId, app.Versions.PendingVersion);
            }

            if (!app.Versions.CurrentVersion.IsNullOrEmpty())
            {
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

            if (!app.Versions.PendingVersion.IsNullOrEmpty())
            {
                await _appDeployService.RestartAppAsync(appId, app.Versions.PendingVersion);
            }

            if (!app.Versions.CurrentVersion.IsNullOrEmpty())
            {
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
        await _appDeployService.UpdateAppDockerImageAsync(input.AppId, input.Version, input.ImageName,
            input.IsUpdateConfig);
    }

    [HttpPost]
    [Route("batch-update-image")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UpdateAppsDockerImageAsync(UpdateAppsDockerImageInput input)
    {
        var tasks = input.AppIds.Select(async appId =>
        {
            var app = await _appService.GetIndexAsync(appId);

            if (!app.Versions.PendingVersion.IsNullOrEmpty())
            {
                await _appDeployService.UpdateAppDockerImageAsync(appId, app.Versions.PendingVersion, input.ImageName,
                    input.IsUpdateConfig);
            }

            if (!app.Versions.CurrentVersion.IsNullOrEmpty())
            {
                await _appDeployService.UpdateAppDockerImageAsync(appId, app.Versions.CurrentVersion, input.ImageName,
                    input.IsUpdateConfig);
            }
        });

        await tasks.WhenAll();
    }

    [HttpGet]
    [Route("pods")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken)
    {
        return await _appDeployService.GetPodListWithPagingAsync(appId, pageSize, continueToken);
    }
    
    // [HttpGet]
    // [Route("prometheus-pod")]
    // [Authorize(Policy = "OnlyAdminAccess")]
    // public async Task<List<AppPodResourceInfoDto>> GetPodResourceInfoAsync(string podName)
    // {
    //     return await _appDeployService.GetPodResourceInfoAsync(podName);
    // }
    
    [HttpPost]
    [Route("destroy-pending")]
    [Authorize]
    public async Task DestroyAppPendingVersionAsync(AppInput input)
    {
        await _appDeployService.DestroyAppPendingVersionAsync(input.AppId);
    }

    [HttpPost]
    [Route("obliterate")]
    [Authorize]
    public async Task ObliterateAppAsync(ObliterateAppInput input)
    {
        await _appDeployService.ObliterateAppAsync(input.AppId, input.OrganizationId);
    }
    
    [HttpPost]
    [Route("unfreeze")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task UnFreezeAppAsync(AppInput input)
    {
        await _appDeployService.UnFreezeAppAsync(input.AppId);
    }
}