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
    [Route("{id}")]
    public async Task CountAsync(string id)
    {
        await _appService.CountAsync(id);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<int> GetCountAsync(string id)
    {
        return await _appService.GetCountAsync(id);
    }
}