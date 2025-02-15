using System;
using System.Threading.Tasks;
using AeFinder.AppResources.Dto;
using Volo.Abp.Application.Dtos;

namespace AeFinder.AppResources;

public interface IAppResourceUsageService
{
    Task AddOrUpdateAsync(AppResourceUsageDto input);
    Task<PagedResultDto<AppResourceUsageDto>> GetListAsync(Guid? organizationId, GetAppResourceUsageInput input);
}