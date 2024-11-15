using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Apps;

public class AppOperationSnapshotProvider : IAppOperationSnapshotProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IAppDeployManager _appDeployManager;

    public AppOperationSnapshotProvider(IClusterClient clusterClient,IAppDeployManager appDeployManager)
    {
        _clusterClient = clusterClient;
        _appDeployManager = appDeployManager;
    }
    
    public async Task SetAppPodOperationSnapshotAsync(string appId, string version, AppPodOperationType operationType)
    {
        var appPodResourceSnapshot = await _appDeployManager.GetPodResourceSnapshotAsync(appId, version);
        appPodResourceSnapshot.PodOperationType = operationType;
        var appPodOperationSnapshotGrain =
            _clusterClient.GetGrain<IAppPodOperationSnapshotGrain>(
                GrainIdHelper.GenerateAppPodOperationSnapshotGrainId(appId, version));
        await appPodOperationSnapshotGrain.SetAsync(appPodResourceSnapshot);
    }
}