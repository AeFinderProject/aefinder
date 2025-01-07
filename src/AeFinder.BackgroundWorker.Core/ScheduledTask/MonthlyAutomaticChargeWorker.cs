using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
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

public class MonthlyAutomaticChargeWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<MonthlyAutomaticChargeWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillingService _billingService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;

    public MonthlyAutomaticChargeWorker(AbpAsyncTimer timer,
        ILogger<MonthlyAutomaticChargeWorker> logger, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IBillingService billingService, 
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _billingService = billingService;
        _organizationInformationProvider = organizationInformationProvider;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.MonthlyAutomaticChargeTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var now = DateTime.UtcNow;
        if (now.Day == _scheduledTaskOptions.MonthlyAutomaticChargeDay)
        {
            _logger.LogInformation($"[MonthlyAutomaticChargeWorker] Today is the {_scheduledTaskOptions.MonthlyAutomaticChargeDay}th day of the month. Executing task.");
            await ProcessMonthlyChargeCheckAsync();
        }
        else
        {
            _logger.LogInformation($"[MonthlyAutomaticChargeWorker] Today is not the {_scheduledTaskOptions.MonthlyAutomaticChargeDay}th day of the month. Task will not be executed.");
        }
    }

    private async Task ProcessMonthlyChargeCheckAsync()
    {
        _logger.LogInformation("[MonthlyAutomaticChargeWorker] Process Monthly Charge Check Async.");
        var now = DateTime.UtcNow;
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[MonthlyAutomaticChargeWorker] Check organization {0} {1}.", organizationName,
                organizationId);
            
            //Check is already exist previous month bill
            var previousMonth = now.AddMonths(-1);
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            var billBeginTime = new DateTime(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0);
            var billEndTime = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, lastDayOfLastMonth.Day,
                23, 59, 59);
            var historySettlementBills = await _billingService.GetListAsync(organizationUnitDto.Id, new GetBillingInput()
            {
                BeginTime = billBeginTime,
                EndTime = billEndTime,
                Type = BillingType.Settlement
            });
            if (historySettlementBills != null && historySettlementBills.TotalCount > 0)
            {
                continue;
            }
            //Get organization wallet address
            var organizationWalletAddress = await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                _logger.LogError($"Organization {organizationId} wallet address is null or empty, please check.");
                continue;
            }

            //Create charge bill
            var settlementBill =
                await _billingService.CreateAsync(organizationUnitDto.Id, BillingType.Settlement, billBeginTime);
            if (settlementBill == null)
            {
                _logger.LogWarning($"[MonthlyAutomaticChargeWorker] the previous settlement bill of organization {organizationName} is null");
                continue;
            }
            
            // //Send transaction to billing contract
            // var sendChargeTransactionOutput = await _billingContractProvider.BillingChargeAsync(organizationWalletAddress, settlementBill.PaidAmount, settlementBill.RefundAmount,
            //     settlementBill.Id.ToString());
            // _logger.LogInformation("[MonthlyAutomaticChargeWorker] Send charge transaction " + sendChargeTransactionOutput.TransactionId +
            //                        " of bill " + settlementBill.Id.ToString());
            // var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
            // // not existed->retry  pending->wait  other->fail
            // int delaySeconds = _contractOptions.DelaySeconds;
            // var chargeTransactionResult = await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
            // var chargeResultQueryRetryTimes = 0;
            // while (chargeTransactionResult.Status == TransactionState.NotExisted &&
            //        chargeResultQueryRetryTimes < _contractOptions.ResultQueryRetryTimes)
            // {
            //     chargeResultQueryRetryTimes++;
            //
            //     await Task.Delay(delaySeconds);
            //     chargeTransactionResult = await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
            // }
            //
            // var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
            //     ? TransactionState.Mined
            //     : TransactionState.Failed;
            // _logger.LogInformation(
            //     $"[MonthlyAutomaticChargeWorker] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
            // if (chargeTransactionStatus == TransactionState.Mined)
            // {
            //     await _billingService.PayAsync(settlementBill.Id, chargeTransactionId, DateTime.UtcNow);
            //     _logger.LogInformation($"Bill {settlementBill.Id.ToString()} is paying.");
            // }
            // else
            // {
            //     _logger.LogError($"Bill {settlementBill.Id.ToString()} payment failed");
            // }
            
        }
    }
}