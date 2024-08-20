using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.AppResources;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("AppResource")]
[Route("api/apps/resources")]
public class AppResourceController : AeFinderController
{
    private readonly IAppResourceService _appResourceService;

    public AppResourceController(IAppResourceService appResourceService)
    {
        _appResourceService = appResourceService;
    }
    
    [HttpGet]
    [Route("{appId}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<List<AppResourceDto>> GetAsync(string appId)
    {
        return await _appResourceService.GetAsync(appId);
    }
}