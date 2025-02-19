using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Email;
using AeFinder.Enums;
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
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class CheckPayFailedBillingWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<CheckPayFailedBillingWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAppEmailSender _appEmailSender;
    private readonly IUserAppService _userAppService;
    private readonly CustomOrganizationOptions _customOrganizationOptions;
    private readonly IApiKeyService _apiKeyService;
    private readonly IBillingManagementService _billingManagementService;
    private readonly IClock _clock;
    private readonly BillingOptions _billingOptions;

    public CheckPayFailedBillingWorker(AbpAsyncTimer timer,
        ILogger<CheckPayFailedBillingWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IClusterClient clusterClient,
        IOrganizationAppService organizationAppService,
        IAssetService assetService,
        IAppDeployService appDeployService,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppEmailSender appEmailSender,
        IUserAppService userAppService,
        IOptionsSnapshot<CustomOrganizationOptions> customOrganizationOptions,
        IApiKeyService apiKeyService,
        IServiceScopeFactory serviceScopeFactory, IBillingManagementService billingManagementService,
        IClock clock, IOptionsSnapshot<BillingOptions> billingOptions) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _assetService = assetService;
        _appDeployService = appDeployService;
        _graphQlOptions = graphQlOptions.Value;
        _appEmailSender = appEmailSender;
        _userAppService = userAppService;
        _customOrganizationOptions = customOrganizationOptions.Value;
        _apiKeyService = apiKeyService;
        _billingManagementService = billingManagementService;
        _clock = clock;
        _billingOptions = billingOptions.Value;
        Timer.Period = scheduledTaskOptions.Value.CheckPayFailedBillingTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organization in organizationUnitList)
        {
            if (organization.OrganizationStatus == OrganizationStatus.Normal &&
                await _billingManagementService.IsPaymentFailedAsync(organization.Id,
                    _clock.Now.AddMonths(_billingOptions.PayFailedBillingCheckMonth)))
            {
                await FrozenOrganizationAsync(organization.Id);
            }
        }
    }

    private async Task FrozenOrganizationAsync(Guid organizationGuid)
    {
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GetOrganizationGrainId(organizationGuid));
        var appIds = await organizationAppGrain.GetAppsAsync();
        var user = await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationGuid);
        //Freeze all apps
        if (appIds != null && appIds.Count > 0)
        {
            _logger.LogInformation(
                $"[FreezeRenewalFailedAssetWorker] These [{string.Join(',', appIds)}] AeIndexers have been found under the organization and are ready to be frozen.");
            foreach (var appId in appIds)
            {
                var appGrain = _clusterClient.GetGrain<IAppGrain>(
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

        //Reset api query limit to free quantity
        var apiQueryAsset = await _assetService.GetListAsync(organizationGuid, new GetAssetInput()
        {
            Type = MerchandiseType.ApiQuery,
            SkipCount = 0,
            MaxResultCount = 1
        });
        if (apiQueryAsset != null && apiQueryAsset.Items.Count > 0)
        {
            var freeQuantity = apiQueryAsset.Items[0].FreeQuantity;
            await _apiKeyService.SetQueryLimitAsync(organizationGuid, freeQuantity);
            _logger.LogInformation($"[FreezeRenewalFailedAssetWorker] The organization {organizationGuid.ToString()} api query limit has been set to free quantity {freeQuantity}.");
        }
        
        //Freeze organization
        await _organizationAppService.FreezeOrganizationAsync(organizationGuid);
        _logger.LogInformation("The organization {OrganizationGuid} has been frozen.", organizationGuid);
    }
}