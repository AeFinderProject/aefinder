using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Commons;
using AeFinder.Email;
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

public class PollingSettlementBillPaymentResultWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<PollingSettlementBillPaymentResultWorker> _logger;
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

    public PollingSettlementBillPaymentResultWorker(AbpAsyncTimer timer,
        ILogger<PollingSettlementBillPaymentResultWorker> logger, 
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
        // Timer.Period = 3 * 60 * 1000; // 180000 milliseconds = 3 minutes
        Timer.Period = _scheduledTaskOptions.SettlementBillPaymentResultPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[PollingSettlementBillPaymentResultWorker] Process indexer polling Async.");
        
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
            
            //Handle settlement bills
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var previousMonth = now.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            var billBeginTime = new DateTime(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0);
            var billEndTime = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, lastDayOfLastMonth.Day,
                23, 59, 59);
            //TODO Just for test, need remove later
            if (organizationId == "28f279dc-fa61-9be9-4994-3a174c683413" 
                || organizationId == "259ea99b-12c0-bb06-f658-3a175c3b6301"
                || organizationId == "179ab110-bc85-d451-b477-3a1760565cec"
                || organizationId == "1aac7746-e277-46e6-414f-3a17610953c4"
                || organizationId == "f5f62f32-2359-0827-1223-3a17613d9124"
                || organizationId == "176b48e6-a241-3242-7b94-3a1761b11f22"
                || organizationId == "5153d905-0f02-ae1c-d070-3a17668a728a"
                || organizationId == "4f89ca02-95c7-0328-fb2f-3a176059cb6a")
            {
                billBeginTime = billBeginTime.AddMonths(1);
                billEndTime = billEndTime.AddMonths(1);
            }
            var settlementBills = await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.Settlement,
                billBeginTime, billEndTime);
            foreach (var settlementBill in settlementBills)
            {
                await HandleSettlementBillAsync(organizationUnitDto.Id, organizationName, settlementBill);
            }
        }
    }
    
    private async Task HandleSettlementBillAsync(Guid organizationGuid,string organizationName,BillingDto settlementBill)
    {
        var organizationId = organizationGuid.ToString();
        if (settlementBill.Status == BillingStatus.Paid)
        {
            return;
        }

        //Pay settlement bill
        if (settlementBill.Status == BillingStatus.Unpaid)
        {
            //Get organization wallet address
            var organizationWalletAddress = await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                _logger.LogError($"Organization {organizationId} wallet address is null or empty, please check.");
                return;
            }

            if (settlementBill.PaidAmount == 0 && settlementBill.RefundAmount == 0)
            {
                _logger.LogWarning($"[PollingSettlementBillPaymentResultWorker] Settlement {settlementBill.Id.ToString()} bill amount is {settlementBill.PaidAmount}. The bill will be considered paid by default.");
                await _billingService.ConfirmPaymentAsync(settlementBill.Id);
                return;
            }
            
            //Send transaction to billing contract
            var sendChargeTransactionOutput = await _billingContractProvider.BillingChargeAsync(organizationWalletAddress, settlementBill.PaidAmount, settlementBill.RefundAmount,
                settlementBill.Id.ToString());
            _logger.LogInformation("[PollingSettlementBillPaymentResultWorker] Send charge transaction " + sendChargeTransactionOutput.TransactionId +
                                   " of bill " + settlementBill.Id.ToString());
            var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
            // not existed->retry  pending->wait  other->fail
            int delaySeconds = _contractOptions.DelaySeconds;
            var chargeTransactionResult = await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
            var chargeResultQueryRetryTimes = 0;
            while (chargeTransactionResult.Status == TransactionState.NotExisted &&
                   chargeResultQueryRetryTimes < _contractOptions.ResultQueryRetryTimes)
            {
                chargeResultQueryRetryTimes++;

                await Task.Delay(delaySeconds);
                chargeTransactionResult = await _billingContractProvider.QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
            }

            var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
                ? TransactionState.Mined
                : TransactionState.Failed;
            _logger.LogInformation(
                $"[PollingSettlementBillPaymentResultWorker] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
            if (chargeTransactionStatus == TransactionState.Mined)
            {
                await _billingService.PayAsync(settlementBill.Id, chargeTransactionId, DateTime.UtcNow);
                _logger.LogInformation($"Bill {settlementBill.Id.ToString()} is paying.");
            }
            else
            {
                _logger.LogError($"Bill {settlementBill.Id.ToString()} payment failed");
            }
        }

        if (settlementBill.Status == BillingStatus.Confirming)
        {
            var billingId = settlementBill.Id.ToString();
            _logger.LogInformation(
                "[PollingSettlementBillPaymentResultWorker] Found confirming {0} bill {1}",
                settlementBill.Type.ToString(), billingId);
            var billTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null, billingId,
                    0,
                    10);
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
                _logger.LogError("[PollingSettlementBillPaymentResultWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[PollingSettlementBillPaymentResultWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
            await _billingService.ConfirmPaymentAsync(settlementBill.Id);
            
            //Send charge successful email
            var userInfo =
                await _userAppService.GetDefaultUserInOrganizationUnitAsync(settlementBill.OrganizationId);
            await _billingEmailSender.SendChargeBalanceSuccessfulNotificationAsync(userInfo.Email,
                transactionResultDto.Address, transactionResultDto.Amount,transactionResultDto.TransactionId
            );

            //Get organization wallet address
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                _logger.LogError($"Organization {organizationId} wallet address is null or empty, please check.");
                return;
            }

            //Create advance payment bill for current month
            var newAdvancePaymentBillTime = DateTime.UtcNow;
            //TODO Just for test, need remove later
            if (organizationId == "28f279dc-fa61-9be9-4994-3a174c683413" 
                || organizationId == "259ea99b-12c0-bb06-f658-3a175c3b6301"
                || organizationId == "179ab110-bc85-d451-b477-3a1760565cec"
                || organizationId == "1aac7746-e277-46e6-414f-3a17610953c4"
                || organizationId == "f5f62f32-2359-0827-1223-3a17613d9124"
                || organizationId == "176b48e6-a241-3242-7b94-3a1761b11f22"
                || organizationId == "5153d905-0f02-ae1c-d070-3a17668a728a"
                || organizationId == "4f89ca02-95c7-0328-fb2f-3a176059cb6a")
            {
                newAdvancePaymentBillTime = newAdvancePaymentBillTime.AddMonths(1);
            }
            var newAdvancePaymentBill = await _billingService.CreateAsync(organizationGuid,
                BillingType.AdvancePayment, newAdvancePaymentBillTime);
            if (newAdvancePaymentBill == null)
            {
                _logger.LogWarning(
                    $"[PollingSettlementBillPaymentResultWorker] create advance bill failed, the current month advance payment bill of organization {organizationId} is null");
                return;
            }

            _logger.LogInformation(
                "[PollingSettlementBillPaymentResultWorker] A monthly {0} bill has been created. Organization: {1} Bill: {2} Lock from amount: {3} BillDate: {4}.",
                BillingType.AdvancePayment.ToString(), organizationName,
                newAdvancePaymentBill.Id.ToString(), newAdvancePaymentBill.PaidAmount, newAdvancePaymentBillTime);

            //Check user organization balance
            var userOrganizationBalanceInfoDto =
                await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
            var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
            string monthFullName = newAdvancePaymentBillTime.ToString("MMMM");
            if (organizationAccountBalance < newAdvancePaymentBill.PaidAmount)
            {
                _logger.LogWarning(
                    $"[PollingSettlementBillPaymentResultWorker] Organization {organizationId} wallet balance {organizationAccountBalance} is not enough to pay advance bill amount {newAdvancePaymentBill.PaidAmount}.");
                //Send email warning
                await _billingEmailSender.SendPreDeductionBalanceInsufficientNotificationAsync(userInfo.Email,monthFullName,
                    organizationName,newAdvancePaymentBill.PaidAmount, organizationAccountBalance,organizationWalletAddress
                );
                _logger.LogInformation($"[PollingSettlementBillPaymentResultWorker] Send balance insufficient notification email to {userInfo.Email}");
            }
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