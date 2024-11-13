using AeFinder.Apps;
using AeFinder.Apps.Dto;

namespace AeFinder.App.Deploy;

public interface IAppResourceLimitProvider
{
    Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId);
    Task SetAppPodOperationSnapshotAsync(string appId, string version, AppPodOperationType operationType);
}