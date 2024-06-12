using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
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
    private readonly IAppLogService _appLogService;

    public AppController(IAppService appService,IAppLogService appLogService)
    {
        _appService = appService;
        _appLogService = appLogService;
    }

    [HttpPost]
    [Authorize]
    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
        return await _appService.CreateAsync(dto);
    }

    [HttpPut]
    [Route("{appId}")]
    [Authorize]
    public async Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto)
    {
        return await _appService.UpdateAsync(appId, dto);
    }

    [HttpGet]
    [Route("{appId}")]
    [Authorize]
    public async Task<AppDto> GetAsync(string appId)
    {
        return await _appService.GetAsync(appId);
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<AppDto>> GetListAsync()
    {
        return await _appService.GetListAsync();
    }
        
    [HttpGet]
    [Route("sync-state/{appId}")]
    public async Task<AppSyncStateDto> GetSyncStateAsync(string appId, string version=null)
    {
        return await _appService.GetSyncStateAsync(appId, version);
    }

    [HttpGet("log")]
    [Authorize]
    public async Task<AppLogRecordDto> GetLatestRealTimeLogs(string startTime, string appId, string version)
    {
        return await _appLogService.GetLatestRealTimeLogs(startTime, appId, version);
    }
}