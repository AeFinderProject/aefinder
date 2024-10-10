using AeFinder.Apps.Dto;

namespace AeFinder.App.Deploy;

public interface IAppDeployManager
{
    Task<string> CreateNewAppAsync(string appId, string version, string imageName, List<string> chainIds);
    Task DestroyAppAsync(string appId, string version, List<string> chainIds);
    Task RestartAppAsync(string appId,string version, List<string> chainIds);
    Task UpdateAppDockerImageAsync(string appId, string version, string newImage, List<string> chainIds, bool isUpdateConfig);
    Task<AppPodsPageResultDto> GetPodListWithPagingAsync(int pageSize, string continueToken);
    Task UpdateAppFullPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, List<string> chainIds);
    Task UpdateAppQueryPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory);
}