using AeFinder.Apps;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppGrain : IGrainWithStringKey
{
    Task<AppDto> CreateAsync(CreateAppDto dto);
    Task<AppDto> UpdateAsync(UpdateAppDto dto);
    Task SetStatusAsync(AppStatus status);
    Task<AppDto> GetAsync();
    Task<string> GetOrganizationIdAsync();
    Task FreezeAppAsync();
    Task UnFreezeAppAsync();
    Task DeleteAppAsync();
    Task SetFirstDeployTimeAsync(DateTime time);
    Task LockAsync(bool isLock);
}