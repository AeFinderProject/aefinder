using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Merchandises;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class ChargeWarningWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<ChargeWarningWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IUserAppService _userAppService;
    private readonly IAppEmailSender _appEmailSender;

    public ChargeWarningWorker(AbpAsyncTimer timer,
        ILogger<ChargeWarningWorker> logger, IClusterClient clusterClient,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IAssetService assetService, IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService, IBillingEmailSender billingEmailSender,
        IUserAppService userAppService,IAppEmailSender appEmailSender,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _assetService = assetService;
        _appDeployService = appDeployService;
        _graphQlOptions = graphQlOptions.Value;
        _billingEmailSender = billingEmailSender;
        _userAppService = userAppService;
        _appEmailSender = appEmailSender;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.ChargeWarningTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessWarningCheckAsync();
    }

    private async Task ProcessWarningCheckAsync()
    {
        _logger.LogInformation("[ChargeWarningWorker] Process Charge Warning Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            _logger.LogInformation("[ChargeWarningWorker] Check organization {0} {1}.", organizationName,
                organizationId);
            
            //Check if it is within the warning period.
            DateTime today = DateTime.Now;
            DateTime lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            int daysUntilEndOfMonth = (lastDayOfMonth - today).Days;
            if (daysUntilEndOfMonth <= _scheduledTaskOptions.RenewalAdvanceWarningDays)
            {
                //Get next month Lock From fee
                var monthTime = DateTime.UtcNow.AddMonths(1);
                var nextMonthFee =
                    await _assetService.CalculateMonthlyCostAsync(organizationUnitDto.Id, monthTime);
                
                //Get organization account balance
                var organizationWalletAddress =
                    await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
                if (organizationWalletAddress.IsNullOrEmpty())
                {
                    _logger.LogWarning($"[ProcessRenewalBalanceCheckAsync] the wallet account of organization {organizationId} is null");
                    continue;
                }
                var userOrganizationBalanceInfoDto = await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
                var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;

                if (organizationAccountBalance < nextMonthFee)
                {
                    //Send email warning
                    string monthFullName = monthTime.ToString("MMMM");
                    var userInfo =
                        await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationUnitDto.Id);
                    await _billingEmailSender.SendPreDeductionBalanceInsufficientNotificationAsync(userInfo.Email,
                        monthFullName, organizationName, nextMonthFee, organizationAccountBalance,
                        organizationWalletAddress
                    );
                    _logger.LogWarning(
                        $"The organization account balance is insufficient to cover the next month cost {nextMonthFee}.");
                }
            }

            //Check if it is within the expiration buffer period
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            if (firstDayOfThisMonth.AddDays(_scheduledTaskOptions.UnpaidBillTimeOutDays) > now)
            {
                continue;
            }
            
            //Check app free asset is expired
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
                        
                        //TODO Check if the current AeIndexer assets are in the cancellation period
                        
                        if (appDto.Status == AppStatus.Frozen || appDto.Status == AppStatus.Deleted)
                        {
                            continue;
                        }

                        if (appId != _graphQlOptions.BillingIndexerId)
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
                        
                        //TODO Check if the current AeIndexer assets are in the cancellation period
                        
                        if (appDto.Status == AppStatus.Frozen || appDto.Status == AppStatus.Deleted)
                        {
                            continue;
                        }
                        
                        if (appId != _graphQlOptions.BillingIndexerId)
                        {
                            await _appDeployService.FreezeAppAsync(appId);
                            await _appEmailSender.SendAeIndexerFreezeNotificationAsync(user.Email, appId);
                        }
                    }
                }
            }
            
            //TODO Set unpaid bills failed
            
            //TODO Check app disk
        }
    }
    
}