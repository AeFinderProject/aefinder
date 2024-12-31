using System;
using System.Threading.Tasks;

namespace AeFinder.Apps;

public interface IAppOperationSnapshotProvider
{
    Task SetAppPodOperationSnapshotAsync(string appId, string version, AppPodOperationType operationType);
    Task<DateTime?> GetAppPodStartTimeAsync(string appId);
}