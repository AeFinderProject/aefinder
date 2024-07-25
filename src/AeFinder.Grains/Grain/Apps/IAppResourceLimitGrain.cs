using AeFinder.Apps.Dto;

namespace AeFinder.Grains.Grain.Apps;

public interface IAppResourceLimitGrain: IGrainWithStringKey
{
    Task<AppResourceLimitDto> GetAsync();

    Task SetAsync(SetAppResourceLimitDto dto);
}