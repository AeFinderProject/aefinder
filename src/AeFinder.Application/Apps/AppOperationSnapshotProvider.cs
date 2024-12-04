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
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;

    public AppOperationSnapshotProvider(IClusterClient clusterClient, IAppDeployManager appDeployManager,
        IAppResourceLimitProvider appResourceLimitProvider)
    {
        _clusterClient = clusterClient;
        _appDeployManager = appDeployManager;
        _appResourceLimitProvider = appResourceLimitProvider;
    }

    public async Task SetAppPodOperationSnapshotAsync(string appId, string version, AppPodOperationType operationType)
    {
        var appPodResourceSnapshot = await _appDeployManager.GetPodResourceSnapshotAsync(appId, version);
        //Use the information from the resource limit configuration directly
        var appResourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        appPodResourceSnapshot.AppFullPodRequestCpuCore = appResourceLimitInfo.AppFullPodRequestCpuCore;
        appPodResourceSnapshot.AppFullPodRequestMemory = appResourceLimitInfo.AppFullPodRequestMemory;
        appPodResourceSnapshot.AppFullPodLimitCpuCore = appResourceLimitInfo.AppFullPodLimitCpuCore;
        appPodResourceSnapshot.AppFullPodLimitMemory = appResourceLimitInfo.AppFullPodLimitMemory;
        appPodResourceSnapshot.AppQueryPodRequestCpuCore = appResourceLimitInfo.AppQueryPodRequestCpuCore;
        appPodResourceSnapshot.AppQueryPodRequestMemory = appResourceLimitInfo.AppQueryPodRequestMemory;
        appPodResourceSnapshot.AppQueryPodReplicas = appResourceLimitInfo.AppPodReplicas;
        appPodResourceSnapshot.PodOperationType = operationType;
        var appPodOperationSnapshotGrain =
            _clusterClient.GetGrain<IAppPodOperationSnapshotGrain>(
                GrainIdHelper.GenerateAppPodOperationSnapshotGrainId(appId, version));
        await appPodOperationSnapshotGrain.SetAsync(appPodResourceSnapshot);
    }
}