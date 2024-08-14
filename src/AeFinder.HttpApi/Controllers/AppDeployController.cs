using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Eto;
using AeFinder.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("AppDeploy")]
[Route("api/app-deploy")]
public class AppDeployController : AeFinderController
{
    private readonly IAppDeployManager _appDeployManager;
    private readonly IDistributedEventBus _distributedEventBus;

    public AppDeployController(IAppDeployManager appDeployManager, IDistributedEventBus distributedEventBus)
    {
        _appDeployManager = appDeployManager;
        _distributedEventBus = distributedEventBus;
    }

    [HttpPost]
    [Route("deploy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task<string> CreateNewAppAsync(CreateNewAppInput input)
    {

        var graphqlUrl = await _appDeployManager.CreateNewAppAsync(input.AppId, input.Version, input.ImageName);
        
        //Publish app pod update eto to background worker
        _distributedEventBus.PublishAsync(new AppPodUpdateEto()
        {
            AppId = input.AppId,
            Version = input.Version,
            DockerImage = input.ImageName
        });
        return graphqlUrl;
    }

    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppAsync(AppVersionInput input)
    {
        await _appDeployManager.DestroyAppAsync(input.AppId, input.Version);
    }

    [HttpPost]
    [Route("restart")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task RestartAppAsync(AppVersionInput input)
    {
        await _appDeployManager.RestartAppAsync(input.AppId, input.Version);
    }
}