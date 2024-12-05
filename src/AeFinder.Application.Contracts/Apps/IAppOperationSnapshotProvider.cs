using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Apps;

public interface IAppOperationSnapshotProvider
{
    Task SetAppPodOperationSnapshotAsync(string appId, string version, AppPodOperationType operationType);
    Task<List<AppPodOperationSnapshotDto>> GetAppPodOperationSnapshotListAsync(string appId);
}