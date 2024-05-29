using System.Threading.Tasks;
using AeFinder.Apps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/apps")]
public class AppController : AeFinderController
{
    private readonly IAppService _appService;

    public AppController(IAppService appService)
    {
        _appService = appService;
    }

    [HttpPost]
    //[Authorize]
    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
        return await _appService.CreateAsync(dto);
    }

    [HttpPut]
    [Route("{appId}")]
    //[Authorize]
    public async Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto)
    {
        return await _appService.UpdateAsync(appId, dto);
    }

    [HttpGet]
    [Route("{appId}")]
    public async Task<AppDto> GetAsync(string appId)
    {
        return await _appService.GetAsync(appId);
    }

    [HttpGet]
    public async Task<PagedResultDto<AppDto>> GetListAsync()
    {
        return await _appService.GetListAsync();
    }
}