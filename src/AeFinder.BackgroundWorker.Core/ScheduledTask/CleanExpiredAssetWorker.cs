using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
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
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
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
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
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
        var now = DateTime.UtcNow;
            
        //Check if it is within the expiration buffer period
        var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
        if (firstDayOfThisMonth.AddDays(_scheduledTaskOptions.UnpaidBillTimeOutDays) > now)
        {
            return;
        }
        
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            
            //Get organization processor assets
            var assets = await _assetService.GetListAsync(organizationUnitDto.Id, new GetAssetInput()
            {
                Category = MerchandiseCategory.Resource,
                Type = MerchandiseType.Processor,
                SkipCount = 0,
                MaxResultCount = 50
            });
            var organizationAppGrain =
                _clusterClient.GetGrain<IOrganizationAppGrain>(
                    GrainIdHelper.GetOrganizationGrainId(organizationId));
            var appIds = await organizationAppGrain.GetAppsAsync();
            var user = await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationUnitDto.Id);
            if (assets == null || assets.TotalCount == 0)
            {
                if (appIds != null && appIds.Count > 0)
                {
                    foreach (var appId in appIds)
                    {
                        var appGrain=_clusterClient.GetGrain<IAppGrain>(
                            GrainIdHelper.GenerateAppGrainId(appId));
                        var appDto = await appGrain.GetAsync();
                        
                        if (appDto.Status == AppStatus.Frozen || appDto.Status == AppStatus.Deleted)
                        {
                            continue;
                        }

                        if (appId != _graphQlOptions.BillingIndexerId &&
                            !_customOrganizationOptions.CustomApps.Contains(appId))
                        {
                            await _appDeployService.FreezeAppAsync(appId);
                            await _appEmailSender.SendAeIndexerFreezeNotificationAsync(user.Email, appId);
                        }
                    }
                }
            }
            else
            {
                //find no asset app
                foreach (var asset in assets.Items)
                {
                    if (appIds.Contains(asset.AppId))
                    {
                        appIds.Remove(asset.AppId);
                    }
                }
                //freeze no asset app
                if (appIds != null && appIds.Count > 0)
                {
                    foreach (var appId in appIds)
                    {
                        var appGrain=_clusterClient.GetGrain<IAppGrain>(
                            GrainIdHelper.GenerateAppGrainId(appId));
                        var appDto = await appGrain.GetAsync();
                        
                        if (appDto.Status == AppStatus.Frozen || appDto.Status == AppStatus.Deleted)
                        {
                            continue;
                        }
                        
                        if (appId != _graphQlOptions.BillingIndexerId &&
                            !_customOrganizationOptions.CustomApps.Contains(appId))
                        {
                            await _appDeployService.FreezeAppAsync(appId);
                            await _appEmailSender.SendAeIndexerFreezeNotificationAsync(user.Email, appId);
                        }
                    }
                }
            }
            
            //TODO Check app disk
            
        }
    }
}