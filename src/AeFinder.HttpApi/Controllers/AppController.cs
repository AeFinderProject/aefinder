using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Kubernetes;
using AeFinder.Models;
using AeFinder.Options;
using AeFinder.User;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/apps")]
public class AppController : AeFinderController
{
    private readonly IAppService _appService;
    private readonly IAppLogService _appLogService;
    private readonly KubernetesOptions _kubernetesOption;
    private readonly IOrganizationAppService _organizationAppService;

    public AppController(IAppService appService, IAppLogService appLogService,
        IOptionsSnapshot<KubernetesOptions> kubernetesOption, IOrganizationAppService organizationAppService)
    {
        _appService = appService;
        _appLogService = appLogService;
        _organizationAppService = organizationAppService;
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
    
    [HttpPut]
    [Route("set-limit/{appId}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<AppResourceLimitDto> SetAppResourceLimitAsync(string appId,SetAppResourceLimitDto dto)
    {
        var appResourceLimitDto = await _appService.SetAppResourceLimitAsync(appId, dto);
        return appResourceLimitDto;
    }
    
    [HttpGet]
    [Route("limit/{appId}")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId)
    {
        return await _appService.GetAppResourceLimitAsync(appId);
    }

    [HttpGet]
    [Route("resource-limits")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<PagedResultDto<AppResourceLimitIndexDto>> GetAppResourceLimitListAsync(
        GetAppResourceLimitInput input)
    {
        return await _appService.GetAppResourceLimitIndexListAsync(input);
    }

    [HttpPut]
    [Route("resource-limits")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task SetAppResourceLimitsAsync(SetAppResourceLimitsInput input)
    {
        var resourceLimitDto = ObjectMapper.Map<SetAppResourceLimitsInput, SetAppResourceLimitDto>(input);
        var tasks = input.AppIds.Select(id => _appService.SetAppResourceLimitAsync(id, resourceLimitDto));

        await tasks.WhenAll();
    }
    
    [HttpGet]
    [Route("resource-pods")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<PagedResultDto<AppPodInfoDto>> GetAppPodResourceInfoListAsync(
        GetAppPodResourceInfoInput input)
    {
        return await _appService.GetAppPodResourceInfoListAsync(input);
    }

    [HttpGet]
    [Route("pods-duration")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<PagedResultDto<AppPodUsageDurationDto>> GetAppPodUsageDurationListAsync(
        GetAppPodUsageDurationInput input)
    {
        return await _appService.GetAppPodUsageDurationListAsync(input);
    }
    
    [HttpGet]
    [Route("search")]
    [Authorize]
    public async Task<ListResultDto<AppInfoImmutable>> SearchAsync(string keyword)
    {
        var orgId = await GetOrganizationIdAsync();
        return await _appService.SearchAsync(orgId, keyword);
    }
    
    private async Task<Guid> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id;
    }

}