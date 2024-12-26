using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.BlockScan;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Market;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Market;
using AeFinder.Metrics;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using AElf.Client.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;
    private readonly IUserAppService _userAppService;
    private readonly IContractProvider _contractProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly AppDeployOptions _appDeployOptions;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly TransactionPollingOptions _transactionPollingOptions;

    public AppDeployService(IClusterClient clusterClient,
        IBlockScanAppService blockScanAppService, IAppDeployManager appDeployManager,
        IUserAppService userAppService, IContractProvider contractProvider,
        IUserInformationProvider userInformationProvider,IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,IOptionsSnapshot<AppDeployOptions> appDeployOptions,
        IOptionsSnapshot<TransactionPollingOptions> transactionPollingOptions,
        IAppOperationSnapshotProvider appOperationSnapshotProvider, IAppResourceLimitProvider appResourceLimitProvider)
    {
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
        _appDeployManager = appDeployManager;
        _appResourceLimitProvider = appResourceLimitProvider;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _userAppService = userAppService;
        _contractProvider = contractProvider;
        _appDeployOptions = appDeployOptions.Value;
        _organizationInformationProvider = organizationInformationProvider;
        _userInformationProvider = userInformationProvider;
        _organizationAppService = organizationAppService;
        _transactionPollingOptions = transactionPollingOptions.Value;
    }

    public async Task<string> DeployNewAppAsync(string appId, string version, string imageName)
    {
        await CheckAppStatusAsync(appId);
        
        var chainIds = await GetDeployChainIdAsync(appId, version);
        var graphqlUrl = await _appDeployManager.CreateNewAppAsync(appId, version, imageName, chainIds);
        await _appOperationSnapshotProvider.SetAppPodOperationSnapshotAsync(appId, version, AppPodOperationType.Start);
        return graphqlUrl;
    }

    public async Task ReDeployAppAsync(string appId)
    {
        await CheckAppStatusAsync(appId);
        
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !allSubscriptions.CurrentVersion.Version.IsNullOrEmpty())
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            var chainIds = await GetDeployChainIdAsync(appId, currentVersion);
            await _appDeployManager.CreateNewAppAsync(appId, currentVersion, _appDeployOptions.AppImageName, chainIds);
            await _appOperationSnapshotProvider.SetAppPodOperationSnapshotAsync(appId, currentVersion, AppPodOperationType.Start);
        }
        if (allSubscriptions.PendingVersion != null && !allSubscriptions.PendingVersion.Version.IsNullOrEmpty())
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            var chainIds = await GetDeployChainIdAsync(appId, pendingVersion);
            await _appDeployManager.CreateNewAppAsync(appId, pendingVersion, _appDeployOptions.AppImageName, chainIds);
            await _appOperationSnapshotProvider.SetAppPodOperationSnapshotAsync(appId, pendingVersion, AppPodOperationType.Start);
        }
        
    }

    public async Task DestroyAppAsync(string appId, string version)
    {
        var chainIds = await GetSubscriptionChainIdAsync(appId, version);
        await _blockScanAppService.PauseAsync(appId, version);
        await _appOperationSnapshotProvider.SetAppPodOperationSnapshotAsync(appId, version, AppPodOperationType.Stop);
        await _appDeployManager.DestroyAppAsync(appId, version, chainIds);
    }

    public async Task RestartAppAsync(string appId, string version)
    {
        var chainIds = await GetDeployChainIdAsync(appId, version);
        await _blockScanAppService.PauseAsync(appId, version);
        await _appDeployManager.RestartAppAsync(appId, version, chainIds);
    }

    public async Task UpdateAppDockerImageAsync(string appId, string version, string imageName, bool isUpdateConfig)
    {
        var chainIds = await GetDeployChainIdAsync(appId, version);
        await _blockScanAppService.PauseAsync(appId, version);
        await _appDeployManager.UpdateAppDockerImageAsync(appId, version, imageName, chainIds, isUpdateConfig);
    }

    private async Task<List<string>> GetSubscriptionChainIdAsync(string appId, string version)
    {
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = await appSubscriptionGrain.GetSubscriptionAsync(version);
        return subscription.SubscriptionItems.Select(o => o.ChainId).ToList();
    }

    private async Task<List<string>> GetDeployChainIdAsync(string appId, string version)
    {
        var chainIds = new List<string>();
        var enableMultipleInstances = (await _appResourceLimitProvider.GetAppResourceLimitAsync(appId)).EnableMultipleInstances;
        if (enableMultipleInstances)
        {
            chainIds = await GetSubscriptionChainIdAsync(appId, version);
        }

        return chainIds;
    }

    public async Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken)
    {
        var podsPageResult = await _appDeployManager.GetPodListWithPagingAsync(appId, pageSize, continueToken);
        return podsPageResult;
    }

    // public async Task<List<AppPodResourceInfoDto>> GetPodResourceInfoAsync(string podName)
    // {
    //     List<string> podsName = new List<string>();
    //     podsName.Add(podName);
    //     var podResourceResult = await _kubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podsName);
    //     return podResourceResult;
    // }
    
    public async Task DestroyAppPendingVersionAsync(string appId)
    {
        //Get organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        //Check App is belong user's organization
        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        var organizationAppGain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(organizationGrainId);
        if (!await organizationAppGain.CheckAppIsExistAsync(appId))
        {
            throw new UserFriendlyException("This app does not belong to the user's organization. Please verify.");
        }
        
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.PendingVersion == null)
        {
            return;
        }

        var version = allSubscriptions.PendingVersion.Version;
        if (string.IsNullOrEmpty(version))
        {
            return;
        }
        var chainIds = await GetSubscriptionChainIdAsync(appId, version);
        await _blockScanAppService.PauseAsync(appId, version);
        await _appOperationSnapshotProvider.SetAppPodOperationSnapshotAsync(appId, version, AppPodOperationType.Stop);
        await _appDeployManager.DestroyAppAsync(appId, version, chainIds);
    }

    public async Task ObliterateAppAsync(string appId,string organizationId)
    {
        Logger.LogInformation($"[ObliterateAppAsync]Obliterate AeIndexer {appId}");
        if (organizationId.IsNullOrEmpty())
        {
            Logger.LogInformation($"User {CurrentUser.Id.ToString()} Obliterate AeIndexer {appId}");
            //Get organization id
            var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
            if (organizationUnit == null)
            {
                throw new UserFriendlyException("User has not yet bind any organization");
            }

            organizationId = organizationUnit.Id.ToString();
        }

        //Check App is belong user's organization
        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        var organizationAppGain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(organizationGrainId);
        if (!await organizationAppGain.CheckAppIsExistAsync(appId))
        {
            throw new UserFriendlyException("This app does not belong to the user's organization. Please verify.");
        }
        
        //Delete app
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        await appGrain.DeleteAppAsync();
        Logger.LogInformation($"[ObliterateAppAsync] App {appId} is deleted.");

        //Clear all subscription
        // await appSubscriptionGrain.ClearGrainStateAsync();

        //remove app info
        // var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        // await _userAppService.DeleteAppAuthentication(appId);
        // await _userAppService.DeleteAppRelatedTokenData(appId);
        // await appGrain.ClearGrainStateAsync();
    }

    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }

    public async Task FreezeAppAsync(string appId)
    {
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !allSubscriptions.CurrentVersion.Version.IsNullOrEmpty())
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await DestroyAppAsync(appId, currentVersion);
        }

        if (allSubscriptions.PendingVersion != null && !allSubscriptions.PendingVersion.Version.IsNullOrEmpty())
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await DestroyAppAsync(appId, pendingVersion);
        }
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        await appGrain.FreezeAppAsync();
    }

    public async Task UnFreezeAppAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        await appGrain.UnFreezeAppAsync();

        var imageName = _appDeployOptions.AppImageName;
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !allSubscriptions.CurrentVersion.Version.IsNullOrEmpty())
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await DeployNewAppAsync(appId, currentVersion, imageName);
        }

        if (allSubscriptions.PendingVersion != null && !allSubscriptions.PendingVersion.Version.IsNullOrEmpty())
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await DeployNewAppAsync(appId, pendingVersion, imageName);
        }
        
    }

    public async Task CheckAppStatusAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var appDto = await appGrain.GetAsync();
        if (appDto.Status == AppStatus.Frozen)
        {
            throw new UserFriendlyException("The AeIndexer renewal has expired and it has been frozen. Please deposit your account first.");
        }

        if (appDto.Status == AppStatus.Deleted)
        {
            throw new UserFriendlyException($"The app is already deleted in {appDto.DeleteTime.ToUniversalTime()}");
        }
    }
    
    

}