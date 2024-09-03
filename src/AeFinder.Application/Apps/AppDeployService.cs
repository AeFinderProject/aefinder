using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.BlockScan;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppDeployService : AeFinderAppService, IAppDeployService
{
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IAppDeployManager _appDeployManager;

    public AppDeployService(IClusterClient clusterClient,
        IBlockScanAppService blockScanAppService, IAppDeployManager appDeployManager)
    {
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
        _appDeployManager = appDeployManager;
    }

    public async Task<string> DeployNewAppAsync(string appId, string version, string imageName)
    {
        var graphqlUrl = await _appDeployManager.CreateNewAppAsync(appId, version, imageName);
        return graphqlUrl;
    }

    public async Task DestroyAppAsync(string appId, string version)
    {
        await _blockScanAppService.PauseAsync(appId, version);
        await _appDeployManager.DestroyAppAsync(appId, version);
    }

    public async Task RestartAppAsync(string appId, string version)
    {
        await _blockScanAppService.PauseAsync(appId, version);
        await _appDeployManager.RestartAppAsync(appId, version);
    }

    public async Task UpdateAppDockerImageAsync(string appId, string version, string imageName, bool isUpdateConfig)
    {
        await _blockScanAppService.PauseAsync(appId, version);
        await _appDeployManager.UpdateAppDockerImageAsync(appId, version, imageName, isUpdateConfig);
    }
}