using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Merchandises;
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
using Volo.Abp.Users;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class BillingIndexerPollingWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<BillingIndexerPollingWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IOrderService _orderService;
    private readonly IBillingService _billingService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly IBillingContractProvider _billingContractProvider;
    private readonly IUserAppService _userAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IAppEmailSender _appEmailSender;
    
    public BillingIndexerPollingWorker(AbpAsyncTimer timer, 
        ILogger<BillingIndexerPollingWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOrderService orderService,
        IBillingService billingService,
        IAssetService assetService,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService,
        IBillingContractProvider billingContractProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IBillingEmailSender billingEmailSender,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _orderService = orderService;
        _billingService = billingService;
        _assetService = assetService;
        _graphQlOptions = graphQlOptions.Value;
        _appDeployService = appDeployService;
        _billingContractProvider = billingContractProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        _billingEmailSender = billingEmailSender;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.BillingIndexerPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[BillingIndexerPollingWorker] Process indexer polling Async.");
        
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
                var users = await _userAppService.GetUsersInOrganizationUnitAsync(organizationUnitDto.Id);
                if (users == null)
                {
                    _logger.LogWarning($"No users under the organization {organizationName}");
                    continue;
                }

                var defaultUser = users.FirstOrDefault();
                var userExtensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(defaultUser.Id);
                if (string.IsNullOrEmpty(userExtensionInfo.WalletAddress))
                {
                    _logger.LogWarning($"The user {defaultUser.Id} has not yet linked a wallet address.");
                    continue;
                }

                organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,
                        userExtensionInfo.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                    continue;
                }
            }
            
            //Handle payment orders
            var paymentOrders = await GetPaymentOrderListAsync(organizationUnitDto.Id);
            foreach (var paymentOrder in paymentOrders)
            {
                await HandlePaymentOrderAsync(organizationUnitDto.Id, paymentOrder);
            }

            //Handle advance payment bills
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var nextMonth = now.AddMonths(1);
            var firstDayOfNextMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
            var lastDayOfThisMonth = firstDayOfNextMonth.AddDays(-1);
            var advanceBillBeginTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
            var advanceBillEndTime = new DateTime(lastDayOfThisMonth.Year, lastDayOfThisMonth.Month, lastDayOfThisMonth.Day,
                23, 59, 59);
            var advancePaymentBills =
                await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.AdvancePayment,
                    advanceBillBeginTime, advanceBillEndTime);
            foreach (var advancePaymentBill in advancePaymentBills)
            {
                await HandleAdvancePaymentBillAsync(advancePaymentBill);
            }
            
            //Handle settlement bills
            var previousMonth = now.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            var billBeginTime = new DateTime(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0);
            var billEndTime = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, lastDayOfLastMonth.Day,
                23, 59, 59);
            var settlementBills = await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.Settlement,
                billBeginTime, billEndTime);
            foreach (var settlementBill in settlementBills)
            {
                await HandleSettlementBillAsync(organizationUnitDto.Id, organizationName, settlementBill);
            }

        }
        
    }

    private async Task HandlePaymentOrderAsync(Guid organizationGuid,OrderDto paymentOrder)
    {
        if (paymentOrder.Status == OrderStatus.Paid || paymentOrder.Status == OrderStatus.Canceled ||
            paymentOrder.Status == OrderStatus.PayFailed)
        {
            return;
        }

        //Automatically failed order that have remained unpaid for a long time
        if (paymentOrder.Status == OrderStatus.Unpaid)
        {
            if (paymentOrder.OrderTime.AddMinutes(_scheduledTaskOptions.UnpaidOrderTimeoutMinutes) <
                DateTime.UtcNow)
            {
                //Set order status to failed
                await _orderService.PaymentFailedAsync(organizationGuid, paymentOrder.Id);
            }
        }

        if (paymentOrder.Status == OrderStatus.Confirming)
        {
            var orderId = paymentOrder.Id.ToString();
            _logger.LogInformation(
                "[BillingIndexerPollingWorker] Found confirming order {1}", orderId);
            var orderTransactionResult =
                await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null,
                    orderId, 0, 10);
            if (orderTransactionResult == null || orderTransactionResult.UserFundRecord == null ||
                orderTransactionResult.UserFundRecord.Items == null ||
                orderTransactionResult.UserFundRecord.Items.Count == 0)
            {
                return;
            }
            
            //Wait until approaching the safe height of LIB before processing
            var transactionResultDto = orderTransactionResult.UserFundRecord.Items[0];
            var currentLatestBlockHeight = await _indexerProvider.GetCurrentVersionSyncBlockHeightAsync();
            if (currentLatestBlockHeight == 0)
            {
                _logger.LogError("[BillingIndexerPollingWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }
            
            //Update bill transaction id & status
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker]Get transaction {transactionResultDto.TransactionId} of order {transactionResultDto.BillingId}");
            await _orderService.ConfirmPaymentAsync(organizationGuid, paymentOrder.Id,
                transactionResultDto.TransactionId,
                transactionResultDto.Metadata.Block.BlockTime);
            
            //Send lock balance successful email
            var userInfo =
                await _userAppService.GetDefaultUserInOrganizationUnitAsync(paymentOrder.OrganizationId);
            await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(userInfo.Email,
                transactionResultDto.Address, transactionResultDto.Amount,transactionResultDto.TransactionId
            );
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
                    $"[BillingIndexerPollingWorker] Organization {organizationId} wallet balance {organizationAccountBalance} is not enough to pay advance bill amount {advancePaymentBill.PaidAmount}.");
                return;
            }

            //Send lockFrom transaction to contract
            var sendLockFromTransactionOutput = await _billingContractProvider.BillingLockFromAsync(
                organizationWalletAddress, advancePaymentBill.PaidAmount,
                advancePaymentBill.Id.ToString());
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker] Send lock from transaction " +
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
                "[BillingIndexerPollingWorker] Found confirming {0} bill {1}",
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
                _logger.LogError("[BillingIndexerPollingWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
            await _billingService.ConfirmPaymentAsync(advancePaymentBill.Id);
            
            //Send lock balance successful email
            var userInfo =
                await _userAppService.GetDefaultUserInOrganizationUnitAsync(advancePaymentBill.OrganizationId);
            await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(userInfo.Email,
                transactionResultDto.Address, transactionResultDto.Amount,transactionResultDto.TransactionId
            );
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
            
            //Send transaction to billing contract
            var sendChargeTransactionOutput = await _billingContractProvider.BillingChargeAsync(organizationWalletAddress, settlementBill.PaidAmount, settlementBill.RefundAmount,
                settlementBill.Id.ToString());
            _logger.LogInformation("[MonthlyAutomaticChargeWorker] Send charge transaction " + sendChargeTransactionOutput.TransactionId +
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
                $"[MonthlyAutomaticChargeWorker] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
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
                "[BillingIndexerPollingWorker] Found confirming {0} bill {1}",
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
                _logger.LogError("[BillingIndexerPollingWorker]Get current latest block height failed");
                return;
            }

            if (currentLatestBlockHeight <
                (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
            {
                return;
            }

            //Update bill transaction id & status
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
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
            var newAdvancePaymentBill = await _billingService.CreateAsync(organizationGuid,
                BillingType.AdvancePayment, newAdvancePaymentBillTime);
            if (newAdvancePaymentBill == null)
            {
                _logger.LogWarning(
                    $"[BillingIndexerPollingWorker] create advance bill failed, the current month advance payment bill of organization {organizationId} is null");
                return;
            }

            //Check user organization balance
            var userOrganizationBalanceInfoDto =
                await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
            var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
            string monthFullName = newAdvancePaymentBillTime.ToString("MMMM");
            if (organizationAccountBalance < newAdvancePaymentBill.PaidAmount)
            {
                //Send email warning
                await _billingEmailSender.SendPreDeductionBalanceInsufficientNotificationAsync(userInfo.Email,monthFullName,
                    organizationName,newAdvancePaymentBill.PaidAmount, organizationAccountBalance,organizationWalletAddress
                );

                //Get organization asset
                var processorAssets = await _assetService.GetListAsync(organizationGuid, new GetAssetInput()
                {
                    Type = MerchandiseType.Processor,
                    SkipCount = 0,
                    MaxResultCount = 50
                });

                //freeze app
                var user = await _userAppService.GetDefaultUserInOrganizationUnitAsync(organizationGuid);
                foreach (var assetDto in processorAssets.Items)
                {
                    var appId = assetDto.AppId;
                    if (appId != _graphQlOptions.BillingIndexerId)
                    {
                        await _appDeployService.FreezeAppAsync(appId);
                        await _appEmailSender.SendAeIndexerFreezeNotificationAsync(user.Email, appId);
                    }
                }

                return;
            }

            //Send lockFrom transaction to contract
            var sendLockFromTransactionOutput = await _billingContractProvider.BillingLockFromAsync(
                organizationWalletAddress, newAdvancePaymentBill.PaidAmount,
                newAdvancePaymentBill.Id.ToString());
            _logger.LogInformation(
                $"[BillingIndexerPollingWorker] Send lock from transaction " +
                sendLockFromTransactionOutput.TransactionId +
                " of bill " + newAdvancePaymentBill.Id.ToString());
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
                await _billingService.PayAsync(newAdvancePaymentBill.Id, lockFromTransactionId, DateTime.UtcNow);
                _logger.LogInformation($"Bill {newAdvancePaymentBill.Id.ToString()} is paying.");
            }
            else
            {
                _logger.LogWarning($"Bill {newAdvancePaymentBill.Id.ToString()} payment failed");

                //Send email warning
                await _billingEmailSender.SendAutoRenewalPreDeductionFailedNotificationAsync(userInfo.Email,
                    monthFullName, organizationName, newAdvancePaymentBill.PaidAmount,
                    newAdvancePaymentBill.PaidAmount);
            }
        }
    }

    private async Task<List<OrderDto>> GetPaymentOrderListAsync(Guid organizationGuid)
    {
        var resultList = new List<OrderDto>();
        var now = DateTime.UtcNow;
        int skipCount = 0;
        int maxResultCount = 10;
        var orderBeginTime = now.AddDays(-1);
        var orderEndTime = now.AddDays(1);
        
        while (true)
        {
            var orders = await _orderService.GetListAsync(organizationGuid, new GetOrderListInput()
            {
                BeginTime = orderBeginTime,
                EndTime = orderEndTime,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
            if (orders?.Items == null || orders.Items.Count == 0)
            {
                break;
            }

            resultList.AddRange(orders.Items);

            if (orders.Items.Count < maxResultCount)
            {
                break;
            }

            skipCount += maxResultCount;
        }
        
        return resultList;
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