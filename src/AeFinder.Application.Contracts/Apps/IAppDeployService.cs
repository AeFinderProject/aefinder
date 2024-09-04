using System.Threading.Tasks;

namespace AeFinder.Apps;

public interface IAppDeployService
{
    Task<string> DeployNewAppAsync(string appId, string version, string imageName);
    Task DestroyAppAsync(string appId, string version);
    Task RestartAppAsync(string appId, string version);
    Task UpdateAppDockerImageAsync(string appId, string version, string imageName);
}