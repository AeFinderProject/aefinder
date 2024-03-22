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
        return _studioService.ApplyAeFinderAppName(ClientId, input);
    }

    [HttpPost("update")]
    [Authorize]
    public Task<AddOrUpdateAeFinderAppDto> AddOrUpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        return _studioService.UpdateAeFinderApp(ClientId, input);
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
        return _studioService.GetAeFinderApp(ClientId, input);
    }

    [HttpGet("applist")]
    [Authorize]
    public Task<List<AeFinderAppInfo>> GetAeFinderAppList()
    {
        return _studioService.GetAeFinderAppList();
    }

    [HttpPost("submitsubscription")]
    [Authorize]
    public Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input)
    {
        return _studioService.SubmitSubscriptionInfoAsync(ClientId, input);
    }

    [HttpPost("query")]
    [Authorize]
    public Task<string> QueryAeFinderApp(QueryAeFinderAppInput input)
    {
        return _studioService.QueryAeFinderAppAsync(ClientId, input);
    }

    [HttpPost("logs")]
    [Authorize]
    public Task<string> QueryAeFinderAppLogs(QueryAeFinderAppLogsInput input)
    {
        return _studioService.QueryAeFinderAppLogsAsync(ClientId, input);
    }
}