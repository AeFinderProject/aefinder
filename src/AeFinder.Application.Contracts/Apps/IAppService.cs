using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Apps;

public interface IAppService
{
    Task<AppDto> CreateAsync(CreateAppDto dto);
    Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto);
    Task<AppDto> GetAsync(string appId);
    Task<PagedResultDto<AppDto>> GetListAsync();
    Task<AppSyncStateDto> GetSyncStateAsync(string appId);
}