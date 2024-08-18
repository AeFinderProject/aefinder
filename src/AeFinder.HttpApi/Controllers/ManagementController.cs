using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Kubernetes;
using AeFinder.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Management")]
[Route("api/management")]
public class ManagementController : AeFinderController
{
    private readonly IAppService _appService;

    public ManagementController(IAppService appService)
    {
        _appService = appService;
    }

    // TODO: The app interface is temporarily placed here, and when the developer interface is unified,
    // it is moved to AppController
    [HttpGet]
    [Route("apps/{appId}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<AppIndexDto> GetAsync(string appId)
    {
        return await _appService.GetIndexAsync(appId);
    }

    [HttpGet]
    [Route("apps")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<PagedResultDto<AppIndexDto>> GetListAsync(GetAppInput input)
    {
        return await _appService.GetIndexListAsync(input);
    }
}