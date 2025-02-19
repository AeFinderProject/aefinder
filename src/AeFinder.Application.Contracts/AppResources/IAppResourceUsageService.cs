using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.AppResources.Dto;
using Volo.Abp.Application.Dtos;

namespace AeFinder.AppResources;

public interface IAppResourceUsageService
{
    Task AddOrUpdateAsync(List<AppResourceUsageDto> input);
    Task DeleteAsync(string appId);
    Task<AppResourceUsageDto> GetAsync(Guid? organizationId, string appId);
    Task<PagedResultDto<AppResourceUsageDto>> GetListAsync(Guid? organizationId, GetAppResourceUsageInput input);
}