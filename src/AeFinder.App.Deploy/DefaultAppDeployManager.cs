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

    public async Task UpdateAppDockerImageAsync(string appId, string version, string imageName, List<string> chainIds)
    {
        return;
    }
}