using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.Assets;
using AeFinder.BlockScan;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Merchandises;
using AeFinder.Metrics;
using AeFinder.Options;
using AeFinder.User;
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
    private readonly IOrganizationAppService _organizationAppService;
    private readonly AppDeployOptions _appDeployOptions;
    private readonly IAssetService _assetService;
    private readonly CustomOrganizationOptions _customOrganizationOptions;
    private readonly IUserAppService _userAppService;
    private readonly IAppEmailSender _appEmailSender;

    public AppDeployService(IClusterClient clusterClient,
        IBlockScanAppService blockScanAppService, IAppDeployManager appDeployManager,
        IOrganizationAppService organizationAppService,
        IOptionsSnapshot<AppDeployOptions> appDeployOptions,
        IOptionsSnapshot<CustomOrganizationOptions> customOrganizationOptions,
        IAssetService assetService,
        IUserAppService userAppService, IAppEmailSender appEmailSender,
        IAppResourceLimitProvider appResourceLimitProvider)
    {
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
        _appDeployManager = appDeployManager;
        _appResourceLimitProvider = appResourceLimitProvider;
        _organizationAppService = organizationAppService;
        _appDeployOptions = appDeployOptions.Value;
        _customOrganizationOptions = customOrganizationOptions.Value;
        _userAppService = userAppService;
        _appEmailSender = appEmailSender;
        _assetService = assetService;
    }

    public async Task<string> DeployNewAppAsync(string appId, string version, string imageName)
    {
        await CheckAppStatusAsync(appId);
        await CheckAppAssetAsync(appId);
        
        var chainIds = await GetDeployChainIdAsync(appId, version);
        var graphqlUrl = await _appDeployManager.CreateNewAppAsync(appId, version, imageName, chainIds);
        await SetFirstDeployTimeAsync(appId);
        return graphqlUrl;
    }

    public async Task DestroyAppAsync(string appId, string version)
    {
        var chainIds = await GetSubscriptionChainIdAsync(appId, version);
        await _blockScanAppService.PauseAsync(appId, version);
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
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainId(organizationId);
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
        await _appDeployManager.DestroyAppAsync(appId, version, chainIds);
    }
    
    public async Task ObliterateAppAsync(string appId,string organizationId)
    {
        if (CurrentUser.IsInRole("admin"))
        {
            Logger.LogInformation($"[ObliterateAppAsync] Admin obliterate AeIndexer {appId} of organization {organizationId}");
        }
        else
        {
            Logger.LogInformation($"[ObliterateAppAsync] User {CurrentUser.Id.ToString()} Obliterate AeIndexer {appId}");
            //Get organization id
            var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
            if (organizationUnit == null)
            {
                throw new UserFriendlyException("User has not yet bind any organization");
            }

            organizationId = organizationUnit.Id.ToString();
        }

        //Check App is belong user's organization
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainId(organizationId);
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
        
        //Send email
        var organizationGuid = Guid.Parse(organizationId);
        var user = await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationGuid);
        await _appEmailSender.SendAeIndexerDeletedNotificationAsync(user.Email, appId);
    }
    
    public async Task FreezeAppAsync(string appId)
    {
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !string.IsNullOrEmpty(allSubscriptions.CurrentVersion.Version))
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await DestroyAppAsync(appId, currentVersion);
        }

        if (allSubscriptions.PendingVersion != null && !string.IsNullOrEmpty(allSubscriptions.PendingVersion.Version))
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await DestroyAppAsync(appId, pendingVersion);
        }
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        await appGrain.FreezeAppAsync();
        Logger.LogInformation($"App {appId} has been frozen.");
    }
    
    public async Task UnFreezeAppAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        await appGrain.UnFreezeAppAsync();

        var imageName = _appDeployOptions.AppImageName;
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !string.IsNullOrEmpty(allSubscriptions.CurrentVersion.Version))
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await DeployNewAppAsync(appId, currentVersion, imageName);
        }

        if (allSubscriptions.PendingVersion != null && !string.IsNullOrEmpty(allSubscriptions.PendingVersion.Version))
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await DeployNewAppAsync(appId, pendingVersion, imageName);
        }
        Logger.LogInformation($"App {appId} is UnFreezed.");
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

    public async Task CheckAppAssetAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var organizationId = await appGrain.GetOrganizationIdAsync();
        Logger.LogDebug($"Check app {appId} asset get organization {organizationId}");
        //Check processor asset
        var processorAssets = await _assetService.GetListAsync(Guid.Parse(organizationId), new GetAssetInput()
        {
            Type = MerchandiseType.Processor,
            AppId = appId,
            SkipCount = 0,
            MaxResultCount = 10,
            IsDeploy = true
        });
        AssetDto processorAsset = null;
        if (processorAssets != null && processorAssets.Items.Count > 0)
        {
            processorAsset = processorAssets.Items.FirstOrDefault();
        }
        
        //If app processor asset is null, check if it is custom organization
        if (processorAsset == null)
        {
            if (!_customOrganizationOptions.CustomApps.Contains(appId))
            {
                throw new UserFriendlyException("Please purchase pod cpu & memory capacity before proceeding with deployment.");
            }

            // await _appResourceLimitProvider.SetAppResourceLimitAsync(appId, new AppResourceLimitDto()
            // {
            //     AppFullPodLimitCpuCore = _customOrganizationOptions.DefaultFullPodLimitCpuCore,
            //     AppFullPodLimitMemory = _customOrganizationOptions.DefaultFullPodLimitMemory
            // });
        }

        //Check storage asset
        var storageAssets = await _assetService.GetListAsync(Guid.Parse(organizationId), new GetAssetInput()
        {
            Type = MerchandiseType.Storage,
            AppId = appId,
            SkipCount = 0,
            MaxResultCount = 10,
            IsDeploy = true
        });
        AssetDto storageAsset = null;
        if (storageAssets != null && storageAssets.Items.Count > 0)
        {
            storageAsset = storageAssets.Items.FirstOrDefault();
        }
        
        //If app storage asset is null, check if it is custom organization
        if (storageAsset == null)
        {
            if (!_customOrganizationOptions.CustomApps.Contains(appId))
            {
                throw new UserFriendlyException("Please purchase storage capacity before proceeding with deployment.");
            }
        }
    }

    private async Task SetFirstDeployTimeAsync(string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var organizationId = await appGrain.GetOrganizationIdAsync();
        var appDto = await appGrain.GetAsync();
        if (appDto.DeployTime == null)
        {
            var now = DateTime.UtcNow;
            await appGrain.SetFirstDeployTimeAsync(now);
            var processorAssets = await _assetService.GetListAsync(Guid.Parse(organizationId), new GetAssetInput()
            {
                Type = MerchandiseType.Processor,
                AppId = appId,
                SkipCount = 0,
                MaxResultCount = 10,
                IsDeploy = true
            });
            AssetDto processorAsset = null;
            if (processorAssets != null && processorAssets.Items.Count > 0)
            {
                processorAsset = processorAssets.Items.FirstOrDefault();
            }

            if (processorAsset != null)
            {
                await _assetService.StartUsingAssetAsync(processorAsset.Id, now);
            }
        }
    }
    
}