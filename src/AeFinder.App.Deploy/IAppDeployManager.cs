using AeFinder.Apps.Dto;

namespace AeFinder.App.Deploy;

public interface IAppDeployManager
{
    Task<string> CreateNewAppAsync(string appId, string version, string imageName, List<string> chainIds);
    Task DestroyAppAsync(string appId, string version, List<string> chainIds);
    Task RestartAppAsync(string appId,string version, List<string> chainIds);
    Task UpdateAppDockerImageAsync(string appId, string version, string newImage, List<string> chainIds, bool isUpdateConfig);
    Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken);
    Task<bool> UpdateAppFullPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, List<string> chainIds, string limitCpu, string limitMemory);
    Task<bool> UpdateAppQueryPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, string limitCpu, string limitMemory, int replicasCount);
    Task<AppPodOperationSnapshotDto> GetPodResourceSnapshotAsync(string appId, string version);
}