using AeFinder.ApiKeys;
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

public class FreezeRenewalFailedAssetWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<FreezeRenewalFailedAssetWorker> _logger;
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
    private readonly IApiKeyService _apiKeyService;
    
    public FreezeRenewalFailedAssetWorker(AbpAsyncTimer timer,
        ILogger<FreezeRenewalFailedAssetWorker> logger,
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
        IApiKeyService apiKeyService,
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
        _billingService = billingService;
        _apiKeyService = apiKeyService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.FreezeRenewalFailedAssetTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var now = DateTime.UtcNow;
        //Check if it is within the expiration buffer period
        var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
        if (firstDayOfThisMonth.AddDays(_scheduledTaskOptions.UnpaidBillTimeOutDays) > now)
        {
            return;
        }
        await ProcessRenewalFailedAssetAsync();
    }

    private async Task ProcessRenewalFailedAssetAsync()
    {
        _logger.LogInformation("[FreezeRenewalFailedAssetWorker] Process renewal failed asset async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            
            //Get advance payment bills
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var nextMonth = now.AddMonths(1);
            var firstDayOfNextMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
            var lastDayOfThisMonth = firstDayOfNextMonth.AddDays(-1);
            var advanceBillBeginTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
            var advanceBillEndTime = new DateTime(lastDayOfThisMonth.Year, lastDayOfThisMonth.Month, lastDayOfThisMonth.Day,
                23, 59, 59);
            
            //TODO just for temp test, need remove later
            if (organizationId == "75dd3f9c-adf6-8ac6-5606-3a177c283f93")
            {
                advanceBillBeginTime = advanceBillBeginTime.AddMonths(1);
                advanceBillEndTime = advanceBillEndTime.AddMonths(1);
            }

            var advancePaymentBills =
                await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.AdvancePayment,
                    BillingStatus.Unpaid, advanceBillBeginTime, advanceBillEndTime);
            
            foreach (var advancePaymentBill in advancePaymentBills)
            {
                _logger.LogInformation($"[FreezeRenewalFailedAssetWorker] Process unpaid advance bill {advancePaymentBill.Id} of organization {organizationName}.");
                await HandleUnpaidAdvanceBillAsync(organizationUnitDto.Id, advancePaymentBill);
            }
        }
    }
    
    private async Task HandleUnpaidAdvanceBillAsync(Guid organizationGuid, BillingDto advancePaymentBill)
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
            MaxResultCount = 10
        });
        if (apiQueryAsset != null && apiQueryAsset.Items.Count > 0)
        {
            var freeQuantity = apiQueryAsset.Items[0].FreeQuantity;
            await _apiKeyService.SetQueryLimitAsync(organizationGuid, freeQuantity);
            _logger.LogInformation($"[FreezeRenewalFailedAssetWorker] The organization {organizationGuid.ToString()} api query limit has been set to free quantity {freeQuantity}.");
        }
        
        //Freeze organization
        await _organizationAppService.FreezeOrganizationAsync(organizationGuid);
        
        //Set bill to payment failed
        await _billingService.PaymentFailedAsync(advancePaymentBill.Id);
        _logger.LogInformation($"[FreezeRenewalFailedAssetWorker] The organization {organizationGuid.ToString()} advance bill {advancePaymentBill.Id.ToString()} payment has been failed.");
    }
    
    private async Task<List<BillingDto>> GetPaymentBillingListAsync(Guid organizationGuid, BillingType type,
        BillingStatus billingStatus,DateTime billBeginTime, DateTime billEndTime)
    {
        var resultList = new List<BillingDto>();
        int skipCount = 0;
        int maxResultCount = 10;

        while (true)
        {
            var bills = await _billingService.GetListAsync(organizationGuid, new GetBillingInput()
            {
                BeginTime = billBeginTime,
                EndTime = billEndTime,
                Type = type,
                Status = billingStatus,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
            if (bills?.Items == null || bills.Items.Count == 0)
            {
                break;
            }

            resultList.AddRange(bills.Items);

            if (bills.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }

        return resultList;
    }
}