using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Apps;

public interface IAppDeployService
{
    Task<string> DeployNewAppAsync(string appId, string version, string imageName);
    Task DestroyAppAsync(string appId, string version);
    Task RestartAppAsync(string appId, string version);
    Task UpdateAppDockerImageAsync(string appId, string version, string imageName, bool isUpdateConfig);
    Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken);
    Task<List<AppPodResourceInfoDto>> GetPodResourceInfoAsync(string podName);
}