using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Apps;

public interface IAppService
{
    Task<AppDto> CreateAsync(CreateAppDto dto);
    Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto);
    Task<AppDto> GetAsync(string appId);
    Task<PagedResultDto<AppDto>> GetListAsync();
    Task<AppIndexDto> GetIndexAsync(string appId);
    Task<PagedResultDto<AppIndexDto>> GetIndexListAsync(GetAppInput input);
    Task<ListResultDto<AppInfoImmutable>> SearchAsync(Guid organizationId, string keyword);
    Task<AppSyncStateDto> GetSyncStateAsync(string appId);
    Task SetMaxAppCountAsync(Guid organizationId, int appCount);
    Task<int> GetMaxAppCountAsync(Guid organizationId);
    Task<string> GetAppCodeAsync(string appId, string version);
    Task<PagedResultDto<AppResourceLimitIndexDto>> GetAppResourceLimitIndexListAsync(GetAppResourceLimitInput input);
    Task<PagedResultDto<AppPodInfoDto>> GetAppPodResourceInfoListAsync(
        GetAppPodResourceInfoInput input);
    Task<PagedResultDto<AppPodUsageDurationDto>> GetAppPodUsageDurationListAsync(GetAppPodUsageDurationInput input);
    Task LockAsync(string appId, bool isLock);
}