using System;
using System.Threading.Tasks;
using AeFinder.AppResources.Dto;
using Volo.Abp.Application.Dtos;

namespace AeFinder.AppResources;

public interface IAppResourceUsageService
{
    Task AddOrUpdateAsync(AppResourceUsageDto input);
    Task DeleteAsync(string appId);
    Task<PagedResultDto<AppResourceUsageDto>> GetListAsync(Guid? organizationId, GetAppResourceUsageInput input);
}