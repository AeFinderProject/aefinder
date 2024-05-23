using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using BrunoZell.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Studio")]
[Route("api/app/studio")]
public class StudioController : AeFinderController
{
    private readonly IStudioService _studioService;

    public StudioController(IStudioService studioService)
    {
        _studioService = studioService;
    }

    [HttpPost("apply")]
    [Authorize]
    public Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input)
    {
        return _studioService.ApplyAeFinderAppNameAsync(input);
    }

    [HttpPost("update")]
    [Authorize]
    public Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderAppAsync(input);
    }

    [HttpGet("info")]
    [Authorize]
    public Task<AeFinderAppInfoDto> GetAeFinderAppInfo()
    {
        return _studioService.GetAeFinderAppAsync();
    }

    [HttpGet("applist")]
    [Authorize]
    public Task<List<AeFinderAppInfo>> GetAeFinderAppList()
    {
        return _studioService.GetAeFinderAppListAsync();
    }

    [HttpPost("submitsubscription")]
    [Authorize]
    public Task<string> SubmitSubscriptionInfoAsync([FromForm] SubscriptionInfo input, [ModelBinder(BinderType = typeof(JsonModelBinder))] SubscriptionManifestDto subscriptionManifest)
    {
        return _studioService.SubmitSubscriptionInfoAsync(input, subscriptionManifest);
    }

    [HttpPost("restartapp")]
    [Authorize]
    public async Task RestartAppAsync(string version)
    {
        await _studioService.RestartAppAsync(version);
    }

    [HttpPost("destroyapp")]
    [Authorize]
    public async Task DestroyAppAsync(string version)
    {
        await _studioService.DestroyAppAsync(version);
    }

    [HttpPost("updateapp")]
    [Authorize]
    public async Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync([FromForm] UpdateAeFinderAppInput input)
    {
        return await _studioService.UpdateAeFinderAppAsync(input);
    }

    [HttpPost("monitorapp/{appId}")]
    public async Task<AppBlockStateMonitorDto> MonitorAppBlockStateAsync(string appId)
    {
        return await _studioService.MonitorAppBlockStateAsync(appId);
    }
}