using System;
using System.Threading.Tasks;
using AeFinder.ApiTraffic;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("ApiTraffic")]
[Route("api/traffic")]
public class ApiTrafficController : AeFinderController
{
    private readonly IApiTrafficService _apiTrafficService;

    public ApiTrafficController(IApiTrafficService apiTrafficService)
    {
        _apiTrafficService = apiTrafficService;
    }
    
    [HttpGet]
    [Route("{key}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<long> GetRequestCountAsync(string key, DateTime date)
    {
        return await _apiTrafficService.GetRequestCountAsync(key, date);
    }
}