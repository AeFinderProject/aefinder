using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using AElf.Client.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class RenewalBillCreateWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IContractProvider _contractProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IAppDeployService _appDeployService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IRenewalService _renewalService;
    private readonly TransactionPollingOptions _transactionPollingOptions;

    public RenewalBillCreateWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService, IClusterClient clusterClient,
        IContractProvider contractProvider, IOrganizationInformationProvider organizationInformationProvider,
        IUserInformationProvider userInformationProvider, IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions, IAppDeployService appDeployService,
        IApiKeyService apiKeyService,
        IRenewalService renewalService,IOptionsSnapshot<TransactionPollingOptions> transactionPollingOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _contractProvider = contractProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _userInformationProvider = userInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _appDeployService = appDeployService;
        _apiKeyService = apiKeyService;
        _renewalService = renewalService;
        _transactionPollingOptions = transactionPollingOptions.Value;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = InitializeNextExecutionDelay();
        _logger.LogInformation($"RenewalBillCreateWorker will run after {Timer.Period/1000} seconds");
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalAsync();
        
        Timer.Period = CalculateNextExecutionDelay();
        _logger.LogInformation($"RenewalBillCreateWorker will run after {Timer.Period/1000} seconds");
    }

    private async Task ProcessRenewalAsync()
    {
        _logger.LogInformation("[RenewalBillCreateWorker] Process Renewal Bill Async.");
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[RenewalBillCreateWorker] Check organization: {0}.", organizationId);
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            var billsGrain =
                _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
            var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
            var renewalList = await renewalGrain.GetAllActiveRenewalInfosAsync(organizationId);
            foreach (var renewalDto in renewalList)
            {
                //Check if the renewal date has arrived
                // if (renewalDto.NextRenewalDate > DateTime.UtcNow.AddDays(1))
                // {
                //     continue;
                // }

                _logger.LogInformation(
                    $"[ProcessRenewalAsync] Start process {renewalDto.ProductType.ToString()} renewal {renewalDto.SubscriptionId} of order {renewalDto.OrderId}");
                var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalDto.ProductId);
                //Skip free product
                if (productInfo.MonthlyUnitPrice == 0)
                {
                    await renewalGrain.UpdateRenewalDateToNextPeriodAsync(renewalDto.SubscriptionId);
                    continue;
                }
                
                //Charge for previous billing cycle
                var latestLockedBill = await billsGrain.GetLatestLockedBillAsync(renewalDto.OrderId);
                var chargeFee = latestLockedBill.BillingAmount;
                decimal refundAmount = 0;
                if (renewalDto.ProductType == ProductType.ApiQueryCount)
                {
                    //Charge based on usage query count
                    var organizationGuid = Guid.Parse(renewalDto.OrganizationId);
                    var monthTime = DateTime.UtcNow.AddMonths(-1);
                    var monthlyQueryCount = await _apiKeyService.GetMonthQueryCountAsync(organizationGuid, monthTime);
                    Logger.LogInformation($"[ProcessRenewalAsync]Api monthly query count:{monthlyQueryCount} time:{monthTime.ToString()}");
                    chargeFee = await billsGrain.CalculateApiQueryMonthlyChargeAmountAsync(monthlyQueryCount);
                    var monthlyFee = renewalDto.ProductNumber * productInfo.MonthlyUnitPrice;
                    refundAmount = monthlyFee - chargeFee;
                }
                _logger.LogInformation(
                    $"[ProcessRenewalAsync] Start process charge for previous billing cycle, chargeFee: {chargeFee} refundAmount: {refundAmount}");
                //Send charge transaction to contract
                var userExtensionDto =
                    await _userInformationProvider.GetUserExtensionInfoByIdAsync(Guid.Parse(renewalDto.UserId));
                if (userExtensionDto.WalletAddress.IsNullOrEmpty())
                {
                    _logger.LogError($"[ProcessRenewalAsync]Please bind user wallet first. user id {renewalDto.UserId}");
                    continue;
                }
                var organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,userExtensionDto.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogError($"[ProcessRenewalAsync]The organization wallet address has not yet been linked to user {renewalDto.UserId}");
                    continue;
                }
                var chargeBill = await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
                {
                    OrganizationId = organizationId,
                    OrderId = renewalDto.OrderId,
                    SubscriptionId = renewalDto.SubscriptionId,
                    ChargeFee = chargeFee,
                    Description = "Auto-renewal charge for the existing order.",
                    RefundAmount = refundAmount
                });
                var sendChargeTransactionOutput = await _contractProvider.BillingChargeAsync(organizationWalletAddress, chargeFee, 0,
                    chargeBill.BillingId);
                _logger.LogInformation("[ProcessRenewalAsync] Send charge transaction " + sendChargeTransactionOutput.TransactionId +
                                       " of bill " + chargeBill.BillingId);
                var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
                // not existed->retry  pending->wait  other->fail
                int delaySeconds = _transactionPollingOptions.DelaySeconds;
                var chargeTransactionResult = await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                var chargeResultQueryRetryTimes = 0;
                while (chargeTransactionResult.Status == TransactionState.NotExisted &&
                       chargeResultQueryRetryTimes < _transactionPollingOptions.RetryTimes)
                {
                    chargeResultQueryRetryTimes++;

                    await Task.Delay(delaySeconds);
                    chargeTransactionResult = await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                }

                var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
                    ? TransactionState.Mined
                    : TransactionState.Failed;
                await billsGrain.UpdateTransactionStatus(chargeBill.BillingId, chargeTransactionStatus);
                _logger.LogInformation(
                    $"[ProcessRenewalAsync] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
                
                
                //Check if the renewal date has arrived
                var today = DateTime.UtcNow;
                // if (renewalDto.NextRenewalDate > today.AddDays(1))
                // {
                //     continue;
                // }
                
                //Check user organization balance
                var userOrganizationBalanceInfoDto =
                    await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                        _contractOptions.BillingContractChainId, 0, 10);
                var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
                
                //If Balance not enough, freeze the AeIndexer, cancel the order & subscription
                if (organizationAccountBalance < renewalDto.PeriodicCost)
                {
                    var ordersGrain =
                        _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
                    await ordersGrain.CancelOrderByIdAsync(renewalDto.OrderId);
                    await renewalGrain.CancelRenewalByIdAsync(renewalDto.SubscriptionId);
                    if (renewalDto.ProductType == ProductType.FullPodResource)
                    {
                        await _appDeployService.FreezeAppAsync(renewalDto.AppId);
                        _logger.LogInformation($"[ProcessRenewalAsync]App {renewalDto.AppId} is frozen.");
                    }

                    if (renewalDto.ProductType == ProductType.ApiQueryCount)
                    {
                        var freeQueryAllowance = await _renewalService.GetUserApiQueryFreeCountAsync(renewalDto.OrganizationId);
                        var organizationGuid = Guid.Parse(renewalDto.OrganizationId);
                        await _apiKeyService.SetQueryLimitAsync(organizationGuid, freeQueryAllowance);
                    }
                    _logger.LogWarning(
                        $"[ProcessRenewalAsync] Organization Account Balance is not enough, organizationAccountBalance: {organizationAccountBalance} PeriodicCost: {renewalDto.PeriodicCost}");
                    continue;
                }
                
                //Lock for next billing cycle
                var lockFee = renewalDto.PeriodicCost;
                var newLockBill = await billsGrain.CreateSubscriptionLockFromBillAsync(new CreateSubscriptionBillDto()
                {
                    OrganizationId = organizationId,
                    SubscriptionId = renewalDto.SubscriptionId,
                    UserId = renewalDto.UserId,
                    AppId = renewalDto.AppId,
                    OrderId = renewalDto.OrderId,
                    Description = $"Auto-renewal lock for the existing order."
                });
                //Send lockFrom transaction to contract
                var sendLockFromTransactionOutput = await _contractProvider.BillingLockFromAsync(organizationWalletAddress, newLockBill.BillingAmount,
                    newLockBill.BillingId);
                _logger.LogInformation(
                    $"[ProcessRenewalAsync] Send lock from transaction " + sendLockFromTransactionOutput.TransactionId +
                " of bill " + newLockBill.BillingId);
                var lockFromTransactionId = sendLockFromTransactionOutput.TransactionId;
                // not existed->retry  pending->wait  other->fail
                var lockFromTransactionResult = await QueryTransactionResultAsync(lockFromTransactionId,delaySeconds);
                var lockFromResultQueryTimes = 0;
                while (lockFromTransactionResult.Status == TransactionState.NotExisted &&
                       lockFromResultQueryTimes < _transactionPollingOptions.RetryTimes)
                {
                    lockFromResultQueryTimes++;

                    await Task.Delay(delaySeconds);
                    lockFromTransactionResult = await QueryTransactionResultAsync(lockFromTransactionId, delaySeconds);
                }

                var status = lockFromTransactionResult.Status == TransactionState.Mined
                    ? TransactionState.Mined
                    : TransactionState.Failed;
                await billsGrain.UpdateTransactionStatus(newLockBill.BillingId, status);
                _logger.LogInformation(
                    $"After {lockFromResultQueryTimes} times retry, get lock from transaction {lockFromTransactionId} status {status}");
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }

    private int InitializeNextExecutionDelay()
    {
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        if (_scheduledTaskOptions.RenewalBillMonth > 0)
        {
            currentMonth = _scheduledTaskOptions.RenewalBillMonth;
        }
        var firstDayNextMonth =
            new DateTime(now.Year, currentMonth, _scheduledTaskOptions.RenewalBillDay,
                    _scheduledTaskOptions.RenewalBillHour, _scheduledTaskOptions.RenewalBillMinute, 0, DateTimeKind.Utc)
                .AddMonths(1);
        _logger.LogInformation("currentMonth: " + currentMonth + " firstDayNextMonth:" + firstDayNextMonth.ToString());
        return (int)(firstDayNextMonth - now).TotalMilliseconds;
    }
    
    private int CalculateNextExecutionDelay()
    {
        var now = DateTime.UtcNow;
        var firstDayNextMonth =
            new DateTime(now.Year, now.Month, _scheduledTaskOptions.RenewalBillDay,
                    _scheduledTaskOptions.RenewalBillHour, _scheduledTaskOptions.RenewalBillMinute, 0, DateTimeKind.Utc)
                .AddMonths(1);
        _logger.LogInformation(" firstDayNextMonth:" + firstDayNextMonth.ToString());
        return (int)(firstDayNextMonth - now).TotalMilliseconds;
    }
    
    private async Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, int delaySeconds)
    {
        // var transactionId = transaction.GetHash().ToHex();
        var transactionResult = await _contractProvider.GetBillingTransactionResultAsync(transactionId);
        while (transactionResult.Status == TransactionState.Pending)
        {
            await Task.Delay(delaySeconds);
            transactionResult = await _contractProvider.GetBillingTransactionResultAsync(transactionId);
        }

        return transactionResult;
    }
}