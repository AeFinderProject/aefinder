using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Options;
using AeFinder.Orders;
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

public class PollingAdvanceBillPaymentResultWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<PollingAdvanceBillPaymentResultWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserAppService _userAppService;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillingService _billingService;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IBillingContractProvider _billingContractProvider;

    public PollingAdvanceBillPaymentResultWorker(AbpAsyncTimer timer,
        ILogger<PollingAdvanceBillPaymentResultWorker> logger, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationInformationProvider organizationInformationProvider,
        IUserAppService userAppService,
        IOrganizationAppService organizationAppService,
        IBillingService billingService,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IBillingEmailSender billingEmailSender,
        IBillingContractProvider billingContractProvider,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationInformationProvider = organizationInformationProvider;
        _userAppService = userAppService;
        _organizationAppService = organizationAppService;
        _billingService = billingService;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _graphQlOptions = graphQlOptions.Value;
        _billingEmailSender = billingEmailSender;
        _billingContractProvider = billingContractProvider;
        // Timer.Period = 5 * 60 * 1000; // 300000 milliseconds = 5 minutes
        Timer.Period = _scheduledTaskOptions.AdvanceBillPaymentResultPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[PollingAdvanceBillPaymentResultWorker] Process indexer polling Async.");
        
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                // _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                continue;
            }
            
            //Handle advance payment bills
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var nextMonth = now.AddMonths(1);
            var firstDayOfNextMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
            var lastDayOfThisMonth = firstDayOfNextMonth.AddDays(-1);
            var advanceBillBeginTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
            var advanceBillEndTime = new DateTime(lastDayOfThisMonth.Year, lastDayOfThisMonth.Month, lastDayOfThisMonth.Day,
                23, 59, 59);
            //TODO Just for test, need remove later
            if (organizationId == "28f279dc-fa61-9be9-4994-3a174c683413" 
                || organizationId == "259ea99b-12c0-bb06-f658-3a175c3b6301"
                || organizationId == "179ab110-bc85-d451-b477-3a1760565cec"
                || organizationId == "4f89ca02-95c7-0328-fb2f-3a176059cb6a")
            {
                advanceBillBeginTime = advanceBillBeginTime.AddMonths(1);
                advanceBillEndTime = advanceBillEndTime.AddMonths(1);
            }
            var advancePaymentBills =
                await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.AdvancePayment,
                    advanceBillBeginTime, advanceBillEndTime);
            foreach (var advancePaymentBill in advancePaymentBills)
            {
                await HandleAdvancePaymentBillAsync(advancePaymentBill);
            }
        }
        
        
    }
    
    private async Task HandleAdvancePaymentBillAsync(BillingDto advancePaymentBill)
    {
        if (advancePaymentBill.Status == BillingStatus.Paid)
        {
            return;
        }

        //Automatically pay bills that have remained unpaid
        if (advancePaymentBill.Status == BillingStatus.Unpaid)
        {
            var organizationId = advancePaymentBill.OrganizationId.ToString();
            //Get organization wallet address
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                _logger.LogError($"Organization {organizationId} wallet address is null or empty, please check.");
                return;
            }

            //Check user organization balance
            var userOrganizationBalanceInfoDto =
                await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
            var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
            if (organizationAccountBalance < advancePaymentBill.PaidAmount)
            {
                _logger.LogWarning(
                    $"[PollingAdvanceBillPaymentResultWorker] Organization {organizationId} wallet balance {organizationAccountBalance} is not enough to pay advance bill amount {advancePaymentBill.PaidAmount}.");
                return;
            }
            
            if (advancePaymentBill.PaidAmount == 0)
            {
                _logger.LogWarning($"[PollingAdvanceBillPaymentResultWorker] Advance {advancePaymentBill.Id.ToString()} bill amount is {advancePaymentBill.PaidAmount}. The bill will be considered paid by default.");
                await _billingService.ConfirmPaymentAsync(advancePaymentBill.Id);
                return;
            }

            //Send lockFrom transaction to contract
            var sendLockFromTransactionOutput = await _billingContractProvider.BillingLockFromAsync(
                organizationWalletAddress, advancePaymentBill.PaidAmount,
                advancePaymentBill.Id.ToString());
            _logger.LogInformation(
                $"[PollingAdvanceBillPaymentResultWorker] Send lock from transaction " +
                sendLockFromTransactionOutput.TransactionId +
                " of bill " + advancePaymentBill.Id.ToString());
            var lockFromTransactionId = sendLockFromTransactionOutput.TransactionId;
            // not existed->retry  pending->wait  other->fail
            int delaySeconds = _contractOptions.DelaySeconds;
            var lockFromTransactionResult =
                await _billingContractProvider.QueryTransactionResultAsync(lockFromTransactionId, delaySeconds);
            var lockFromResultQueryTimes = 0;
            while (lockFromTransactionResult.Status == TransactionState.NotExisted &&
                   lockFromResultQueryTimes < _contractOptions.ResultQueryRetryTimes)
            {
                lockFromResultQueryTimes++;

                await Task.Delay(delaySeconds);
                lockFromTransactionResult =
                    await _billingContractProvider.QueryTransactionResultAsync(lockFromTransactionId, delaySeconds);
            }

            var lockFromTransactionStatus = lockFromTransactionResult.Status == TransactionState.Mined
                ? TransactionState.Mined
                : TransactionState.Failed;
            _logger.LogInformation(
                $"After {lockFromResultQueryTimes} times retry, get lock from transaction {lockFromTransactionId} status {lockFromTransactionStatus}");
            if (lockFromTransactionStatus == TransactionState.Mined)
            {
                await _billingService.PayAsync(advancePaymentBill.Id, lockFromTransactionId, DateTime.UtcNow);
                _logger.LogInformation($"Bill {advancePaymentBill.Id.ToString()} is paying.");
            }
            else
            {
                _logger.LogWarning($"Bill {advancePaymentBill.Id.ToString()} payment failed");
            }
        }

        if (advancePaymentBill.Status == BillingStatus.Confirming)
        {
            var billingId = advancePaymentBill.Id.ToString();
            _logger.LogInformation(
                "[PollingAdvanceBillPaymentResultWorker] Found confirming {0} bill {1}",
                advancePaymentBill.Type.ToString(), billingId);
            var billTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null,
                    billingId, 0, 10);
            if (billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
                billTransactionResult.UserFundRecord.Items == null ||
                billTransactionResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }

            //Wait until approaching the safe height of LIB before processing
            var transactionResultDto = billTransactionResult.UserFundRecord.Items[0];
            var currentLatestBlockHeight = await _indexerProvider.GetCurrentVersionSyncBlockHeightAsync();
            if (currentLatestBlockHeight == 0)
            {
                _logger.LogError("[PollingAdvanceBillPaymentResultWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[PollingAdvanceBillPaymentResultWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
            await _billingService.ConfirmPaymentAsync(advancePaymentBill.Id);
            
            //Send lock balance successful email
            var userInfo =
                await _userAppService.GetDefaultUserInOrganizationUnitAsync(advancePaymentBill.OrganizationId);
            await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(userInfo.Email,
                transactionResultDto.Address, transactionResultDto.Amount,transactionResultDto.TransactionId
            );
        }
    }
    
    private async Task<List<BillingDto>> GetPaymentBillingListAsync(Guid organizationGuid, BillingType type,
        DateTime billBeginTime, DateTime billEndTime)
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