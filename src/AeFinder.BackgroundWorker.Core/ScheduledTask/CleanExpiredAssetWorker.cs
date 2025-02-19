using AeFinder.AppResources;
using AeFinder.AppResources.Dto;
using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Merchandises;
using AeFinder.Options;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class CleanExpiredAssetWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<CleanExpiredAssetWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAppEmailSender _appEmailSender;
    private readonly IUserAppService _userAppService;
    private readonly CustomOrganizationOptions _customOrganizationOptions;
    private readonly IBillingService _billingService;
    private readonly IAppResourceUsageService _appResourceUsageService;
    
    public CleanExpiredAssetWorker(AbpAsyncTimer timer,
        ILogger<CleanExpiredAssetWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IClusterClient clusterClient,
        IOrganizationAppService organizationAppService,
        IAssetService assetService,
        IAppDeployService appDeployService,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppEmailSender appEmailSender,
        IUserAppService userAppService,
        IOptionsSnapshot<CustomOrganizationOptions> customOrganizationOptions,
        IBillingService billingService,
        IServiceScopeFactory serviceScopeFactory, IAppResourceUsageService appResourceUsageService) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _assetService = assetService;
        _appDeployService = appDeployService;
        _graphQlOptions = graphQlOptions.Value;
        _appEmailSender = appEmailSender;
        _userAppService = userAppService;
        _customOrganizationOptions = customOrganizationOptions.Value;
        _billingService = billingService;
        _appResourceUsageService = appResourceUsageService;
        // Timer.Period = 1 * 60 * 60 * 1000; // 3600000 milliseconds = 1 hours
        Timer.Period = _scheduledTaskOptions.CleanExpiredAssetTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessAssetCleanAsync();
    }

    private async Task ProcessAssetCleanAsync()
    {
        _logger.LogInformation("[CleanExpiredAssetWorker] Process Expired Asset Clean Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            
            var user = await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationUnitDto.Id);
            
            
            //Get organization apps
            var organizationAppGrain =
                _clusterClient.GetGrain<IOrganizationAppGrain>(
                    GrainIdHelper.GetOrganizationGrainId(organizationId));
            var appIds = await organizationAppGrain.GetAppsAsync();
            foreach (var appId in appIds)
            {
                var appGrain = _clusterClient.GetGrain<IAppGrain>(
                    GrainIdHelper.GenerateAppGrainId(appId));
                var appDto = await appGrain.GetAsync();
                if (appDto.DeployTime == null)
                {
                    continue;
                }
                
                if (appDto.Status == AppStatus.UnDeployed)
                {
                    continue;
                }
                
                if (appId == _graphQlOptions.BillingIndexerId ||
                    _customOrganizationOptions.CustomApps.Contains(appId))
                {
                    continue;
                }
                
                //Get organization processor assets
                var appAssets = await GetAeIndexerAssetListAsync(organizationUnitDto.Id, appId);
                if (appAssets == null || appAssets.Count == 0)
                {
                    // Destroy app pod
                    await _appDeployService.DestroyAppAllSubscriptionAsync(appId);
                    continue;
                }

                //Check app if contains both processor and storage
                bool isContainProcessorAsset =
                    appAssets.Exists(a => a.AppId == appId && a.Merchandise.Type == MerchandiseType.Processor);
                bool isContainStorageAsset =
                    appAssets.Exists(a => a.AppId == appId && a.Merchandise.Type == MerchandiseType.Storage);
                if (isContainProcessorAsset && isContainStorageAsset)
                {
                    await CheckAssetExpiredAsync(appAssets, user.Email);
                }
                else
                {
                    _logger.LogInformation($"App {appId} contain processor {isContainProcessorAsset}, storage {isContainStorageAsset}, need to stop pod.");
                    // Destroy app pod
                    await _appDeployService.DestroyAppAllSubscriptionAsync(appId);
                    continue;
                }

                await CheckAppStorageAsync(appId, appAssets);
            }

        }
    }

    private async Task CheckAssetExpiredAsync(List<AssetDto> appAssets, string email)
    {
        var now = DateTime.UtcNow;
        foreach (var asset in appAssets)
        {
            if (asset.Merchandise.Type == MerchandiseType.ApiQuery)
            {
                continue;
            }

            if (asset.Merchandise.Type == MerchandiseType.Processor ||
                asset.Merchandise.Type == MerchandiseType.Storage)
            {
                if (asset.Status == AssetStatus.Using)
                {
                    if (asset.EndTime < now)
                    {
                        var appId = asset.AppId;
                        _logger.LogInformation(
                            "App {0} asset {1} valid until {2}, but the current date is {3}. The asset has expired, and the App needs to stop its pod.",
                            appId, asset.Merchandise.Type.ToString(), asset.EndTime.ToString(), now.ToString());
                        // Destroy app pod
                        await _appDeployService.DestroyAppAllSubscriptionAsync(appId);
                        return;
                    }
                }
            }
        }
    }

    private async Task FreezeAppAsync(string appId, string email)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(
            GrainIdHelper.GenerateAppGrainId(appId));
        var appDto = await appGrain.GetAsync();

        if (appDto.Status == AppStatus.Frozen || appDto.Status == AppStatus.Deleted)
        {
            return;
        }

        if (appId != _graphQlOptions.BillingIndexerId &&
            !_customOrganizationOptions.CustomApps.Contains(appId))
        {
            await _appDeployService.FreezeAppAsync(appId);
            await _appEmailSender.SendAeIndexerFreezeNotificationAsync(email, appId);
        }
    }

    private async Task<List<AssetDto>> GetAeIndexerAssetListAsync(Guid organizationGuid, string appId)
    {
        var resultList = new List<AssetDto>();
        int skipCount = 0;
        int maxResultCount = 10;

        while (true)
        {
            var assets = await _assetService.GetListAsync(organizationGuid, new GetAssetInput()
            {
                AppId = appId,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
            if (assets?.Items == null || assets.Items.Count == 0)
            {
                break;
            }

            resultList.AddRange(assets.Items);

            if (assets.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }

        return resultList;
    }

    private async Task CheckAppStorageAsync(string appId, List<AssetDto> appAssets)
    {
        var appResourceUsage = await _appResourceUsageService.GetAsync(null, appId);
        
        if (appResourceUsage!= null)
        {
            var storageAsset =
                appAssets.First(o => o.Status == AssetStatus.Using && o.Merchandise.Type == MerchandiseType.Storage);

            foreach (var resourceUsage in appResourceUsage.ResourceUsages.Values
                         .SelectMany(resourceUsages => resourceUsages.Where(resourceUsage =>
                             resourceUsage.Name == AeFinderApplicationConsts.AppStorageResourceName)))
            {
                if (Convert.ToDecimal(resourceUsage.Usage) <= storageAsset.Replicas)
                {
                    continue;
                }

                _logger.LogInformation(
                    "App {App}: storage usage ({StorageUsage} GB) exceeds the purchased amount ({StorageAsset} GB).",
                    appId, resourceUsage.Usage, storageAsset.Replicas);
                await _appDeployService.DestroyAppAllSubscriptionAsync(appId);
                return;
            }
        }
    }
}