using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Commons;
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
    
    public ChargeWarningWorker(AbpAsyncTimer timer, 
        ILogger<ChargeWarningWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IAssetService assetService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
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
                //TODO Get next month Lock From fee
                
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
            }

            //Check app free asset is expired
            var assets = await _assetService.GetListsAsync(organizationUnitDto.Id, new GetAssetInput()
            {
                Category = MerchandiseCategory.Resource,
                Type = MerchandiseType.Processor
            });
            if (assets == null || assets.TotalCount == 0)
            {
                continue;
            }

            foreach (var asset in assets.Items)
            {
            }
        }
    }
}