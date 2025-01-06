using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.AppResources;

public interface IAppResourceService
{
    Task<List<AppResourceDto>> GetAsync(string appId);
    Task<AppResourceLimitDto> SetAppResourceLimitAsync(string appId, SetAppResourceLimitDto dto);
    Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId);
}