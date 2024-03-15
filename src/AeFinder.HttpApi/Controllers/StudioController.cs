using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Studio;
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

    [HttpGet("apply")]
    [Authorize]
    public Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput input)
    {
        return _studioService.ApplyAeFinderAppName(input);
    }

    [HttpPost("update")]
    [Authorize]
    public Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderApp(input);
    }

    [HttpPost("adddeveloper")]
    [Authorize]
    public Task<AddDeveloperToAppDto> AddDeveloperToApp(AddDeveloperToAppInput input)
    {
        return _studioService.AddDeveloperToApp(input);
    }

    [HttpPost("info")]
    [Authorize]
    public Task<AeFinderAppInfoDto> GetAeFinderAppInfo(GetAeFinderAppInfoInput input)
    {
        return _studioService.GetAeFinderApp(input);
    }

    [HttpGet("applist")]
    [Authorize]
    public Task<List<AeFinderAppInfo>> GetAeFinderAppList()
    {
        return _studioService.GetAeFinderAppList();
    }

    [HttpPost]
    [Authorize]
    public virtual Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input)
    {
        return _studioService.SubmitSubscriptionInfoAsync(input);
    }
}