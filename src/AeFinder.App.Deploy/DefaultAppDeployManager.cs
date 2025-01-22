using AeFinder.Apps.Dto;

namespace AeFinder.App.Deploy;

public class DefaultAppDeployManager : IAppDeployManager
{
    public async Task<string> CreateNewAppAsync(string appId, string version, string imageName, List<string> chainIds)
    {
        return string.Empty;
    }

    public async Task DestroyAppAsync(string appId, string version, List<string> chainIds)
    {
        return;
    }

    public async Task RestartAppAsync(string appId, string version, List<string> chainIds)
    {
        return;
    }

    public async Task UpdateAppDockerImageAsync(string appId, string version, string imageName, List<string> chainIds,
        bool isUpdateConfig)
    {
        return;
    }

    public async Task<bool> UpdateAppFullPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, List<string> chainIds, string limitCpu, string limitMemory)
    {
        return true;
    }

    public async Task<bool> UpdateAppQueryPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, string limitCpu, string limitMemory, int replicasCount)
    {
        return true;
    }

    public async Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken)
    {
        return new AppPodsPageResultDto();
    }

    public async Task<AppPodOperationSnapshotDto> GetPodResourceSnapshotAsync(string appId, string version)
    {
        return new AppPodOperationSnapshotDto();
    }
}