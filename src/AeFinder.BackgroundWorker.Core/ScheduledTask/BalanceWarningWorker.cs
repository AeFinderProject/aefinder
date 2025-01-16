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

public class BalanceWarningWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<BalanceWarningWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IAssetService _assetService;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IUserAppService _userAppService;

    public BalanceWarningWorker(AbpAsyncTimer timer,
        ILogger<BalanceWarningWorker> logger, IClusterClient clusterClient,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IAssetService assetService, 
        IBillingEmailSender billingEmailSender,
        IUserAppService userAppService,
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
        _billingEmailSender = billingEmailSender;
        _userAppService = userAppService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.BalanceWarningTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        //Check if it is within the warning period.
        DateTime today = DateTime.UtcNow;
        DateTime lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        int daysUntilEndOfMonth = (lastDayOfMonth - today).Days;
        if (daysUntilEndOfMonth > _scheduledTaskOptions.RenewalAdvanceWarningDays)
        { 
            return;    
        }
        
        await ProcessWarningCheckAsync();
    }

    private async Task ProcessWarningCheckAsync()
    {
        _logger.LogInformation("[BalanceWarningWorker] Process Charge Warning Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            _logger.LogInformation("[BalanceWarningWorker] Check organization {0} {1}.", organizationName,
                organizationId);


            //Get next month Lock From fee
            var monthTime = DateTime.UtcNow.AddMonths(1);
            var nextMonthFee =
                await _assetService.CalculateMonthlyCostAsync(organizationUnitDto.Id, monthTime);

            //Get organization account balance
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (organizationWalletAddress.IsNullOrEmpty())
            {
                _logger.LogWarning(
                    $"[BalanceWarningWorker] the wallet account of organization {organizationId} is null");
                continue;
            }

            var userOrganizationBalanceInfoDto = await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                _contractOptions.BillingContractChainId, 0, 10);
            var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;

            if (organizationAccountBalance < nextMonthFee)
            {
                _logger.LogWarning(
                    $"[BalanceWarningWorker] The organization account balance is insufficient to cover the next month cost {nextMonthFee}.");
                //Send email warning
                string monthFullName = monthTime.ToString("MMMM");
                var userInfo =
                    await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationUnitDto.Id);
                await _billingEmailSender.SendPreDeductionBalanceInsufficientNotificationAsync(userInfo.Email,
                    monthFullName, organizationName, nextMonthFee, organizationAccountBalance,
                    organizationWalletAddress
                );
            }
            
        }
    }
    
}