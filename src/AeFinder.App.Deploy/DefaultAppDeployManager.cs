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

    public async Task UpdateAppFullPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, List<string> chainIds)
    {
        return;
    }

    public async Task UpdateAppQueryPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory)
    {
        return;
    }

    public async Task<AppPodsPageResultDto> GetPodListWithPagingAsync(int pageSize, string continueToken)
    {
        return new AppPodsPageResultDto();
    }
}