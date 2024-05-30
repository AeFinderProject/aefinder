using System;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Subscriptions;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SubscriptionAppService : AeFinderAppService, ISubscriptionAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ICodeAuditor _codeAuditor;
    private readonly IAppDeployManager _appDeployManager;
    private readonly AppDeployOptions _appDeployOptions;

    public SubscriptionAppService(IClusterClient clusterClient, ICodeAuditor codeAuditor,
        IAppDeployManager appDeployManager, IOptionsSnapshot<AppDeployOptions> appDeployOptions)
    {
        _clusterClient = clusterClient;
        _codeAuditor = codeAuditor;
        _appDeployManager = appDeployManager;
        _appDeployOptions = appDeployOptions.Value;
    }

    public async Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code)
    {
        AuditCode(code);
        
        var subscription = ObjectMapper.Map<SubscriptionManifestDto, SubscriptionManifest>(manifest);
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));

        var addResult = await appSubscriptionGrain.AddSubscriptionV2Async(subscription, code);
        
        if (!addResult.StopVersion.IsNullOrEmpty())
        {
            await _appDeployManager.DestroyAppAsync(appId, addResult.StopVersion);
            Logger.LogInformation("App stopped. AppId: {appId}, Version: {version}", appId, addResult.StopVersion);
        }
        
        var rulePath = await _appDeployManager.CreateNewAppAsync(appId, addResult.NewVersion, _appDeployOptions.AppImageName);
        Logger.LogInformation("App deployed. AppId: {appId}, Version: {version}, RulePath: {rulePath}", appId, addResult.NewVersion, rulePath);
        return addResult.NewVersion;
    }

    public Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest)
    {
        throw new System.NotImplementedException();
    }

    public async Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code)
    {
        AuditCode(code);
        
        var subscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await subscriptionGrain.UpdateCodeAsync(version, code);
        await _appDeployManager.RestartAppAsync(appId, version);
        Logger.LogInformation("App updated. AppId: {appId}, Version: {version}", appId, version);
    }

    public async Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId)
    {
        var clientGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await clientGrain.GetAllSubscriptionAsync();
        return ObjectMapper.Map<AllSubscription, AllSubscriptionDto>(allSubscription);
    }

    private void AuditCode(byte[] code)
    {
        try
        {
            _codeAuditor.Audit(code);
        }
        catch (CodeCheckException ex)
        {
            throw new UserFriendlyException(ex.Message);
        }
    }
}