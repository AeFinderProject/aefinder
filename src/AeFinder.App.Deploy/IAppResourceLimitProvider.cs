using AeFinder.Apps.Dto;

namespace AeFinder.App.Deploy;

public interface IAppResourceLimitProvider
{
    Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId);
}