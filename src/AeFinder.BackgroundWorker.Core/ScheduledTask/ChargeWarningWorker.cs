using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Commons;
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
    
    public ChargeWarningWorker(AbpAsyncTimer timer, 
        ILogger<ChargeWarningWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IAssetService assetService,IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService,
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
            _logger.LogInformation("[ChargeWarningWorker] Check organization {0} {1}.", organizationName,
                organizationId);
            
            //Check if it is within the warning period.
            DateTime today = DateTime.Now;
            DateTime lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            int daysUntilEndOfMonth = (lastDayOfMonth - today).Days;
            if (daysUntilEndOfMonth <= _scheduledTaskOptions.RenewalAdvanceWarningDays)
            {
                //Get next month Lock From fee
                var currentMonthFee =
                    await _assetService.CalculateMonthlyCostAsync(organizationUnitDto.Id, DateTime.UtcNow);
                
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

                if (organizationAccountBalance < currentMonthFee)
                {
                    //TODO Send email warning
                    
                    _logger.LogWarning(
                        $"The organization account balance is insufficient to cover the current pre-deduction amount {currentMonthFee}. Please top up in time.");
                }
            }

            //Check app free asset is expired
            var assets = await _assetService.GetListsAsync(organizationUnitDto.Id, new GetAssetInput()
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
            if (assets == null || assets.TotalCount == 0)
            {
                if (appIds != null && appIds.Count > 0)
                {
                    foreach (var appId in appIds)
                    {
                        if (appId != _graphQlOptions.BillingIndexerId)
                        {
                            await _appDeployService.FreezeAppAsync(appId);
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
                        if (appId != _graphQlOptions.BillingIndexerId)
                        {
                            await _appDeployService.FreezeAppAsync(appId);
                        }
                    }
                }
            }
            
            
            //TODO Check app disk
        }
    }
    
}