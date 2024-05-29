using System;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppService : AeFinderAppService, IAppService
{
    private readonly IClusterClient _clusterClient;

    public AppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
        dto.AppId = dto.AppName.ToLower().Replace(" ", "_");
        dto.DeployKey = Guid.NewGuid().ToString("N");
        
        // TODO: register appid and deploy key
        
        // TODO: add Org->app
        
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateGrainId(dto.AppId));
        return await appGrain.CreateAsync(dto);
    }

    public async Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto)
    {
        // TODO: check Org->app, whether the app belongs to the current user
        
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateGrainId(appId));
        return await appGrain.UpdateAsync(dto);
    }

    public async Task<AppDto> GetAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateGrainId(appId));
        var app = await appGrain.GetAsync();

        var subscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var versions = await subscriptionGrain.GetAllSubscriptionAsync();
        app.Versions.CurrentVersion = versions.CurrentVersion?.Version;
        app.Versions.PendingVersion = versions.NewVersion?.Version;

        return app;
    }

    public async Task<PagedResultDto<AppDto>> GetListAsync()
    {
        throw new System.NotImplementedException();
    }
}