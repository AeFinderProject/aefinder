using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Kubernetes;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    private readonly KubernetesOptions _kubernetesOption;

    public AppController(IAppService appService, IAppLogService appLogService,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption)
    {
        _appService = appService;
        _appLogService = appLogService;
        _kubernetesOption = kubernetesOption.Value;
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
    public async Task<AppSyncStateDto> GetSyncStateAsync(string appId)
    {
        return await _appService.GetSyncStateAsync(appId);
    }

    [HttpGet("log")]
    [Authorize]
    public async Task<List<AppLogRecordDto>> GetLatestRealTimeLogs(string startTime, string appId, string version,
        List<string> levels = null, string logId = null, string searchKeyWord = null, string chainId = null)
    {
        return await _appLogService.GetLatestRealTimeLogs(_kubernetesOption.AppNameSpace, startTime, appId, version,
            levels, logId, searchKeyWord, chainId);
    }

    [HttpGet("code")]
    // [Authorize]
    public async Task<string> GetAppCodeAsync(string appId, string version)
    {
        return await _appService.GetAppCodeAsync(appId, version);
    }
}