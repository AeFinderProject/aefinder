namespace AeFinder.App.Deploy;

public interface IAppDeployManager
{
    Task<string> CreateNewAppAsync(string appId, string version, string imageName);
    Task DestroyAppAsync(string appId, string version);
    Task RestartAppAsync(string appId,string version);
    Task DestroyAppFullPodsAsync(string appId, string version);
    Task DestroyAppQueryPodsAsync(string appId, string version);
    Task UpdateAppDockerImageAsync(string appId, string version, string newImage);
}