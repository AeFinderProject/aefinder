using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Kubernetes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/test")]
public class TestController : AeFinderController
{
    private readonly IAppService _appService;

    public TestController(IAppService appService)
    {
        _appService = appService;
    }

    [HttpPost]
    public async Task CountAsync()
    {
        await _appService.CountAsync();
    }

    [HttpGet]
    public async Task<int> GetCountAsync()
    {
        return await _appService.GetCountAsync();
    }
}