using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
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
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class RenewalBalanceCheckWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAppDeployService _appDeployService;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IApiKeyService _apiKeyService;
    private readonly IRenewalService _renewalService;
    private readonly IContractProvider _contractProvider;
    private readonly TransactionPollingOptions _transactionPollingOptions;

    public RenewalBalanceCheckWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService, IAeFinderIndexerProvider indexerProvider,
        IOrganizationAppService organizationAppService, IClusterClient clusterClient,
        IOrganizationInformationProvider organizationInformationProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IApiKeyService apiKeyService, IRenewalService renewalService,
        IContractProvider contractProvider, IOptionsSnapshot<TransactionPollingOptions> transactionPollingOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _graphQlOptions = graphQlOptions.Value;
        _appDeployService = appDeployService;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _organizationInformationProvider = organizationInformationProvider;
        _apiKeyService = apiKeyService;
        _renewalService = renewalService;
        _contractProvider = contractProvider;
        _transactionPollingOptions = transactionPollingOptions.Value;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.RenewalBalanceCheckTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalBalanceCheckAsync();
    }

    private async Task ProcessRenewalBalanceCheckAsync()
    {
        _logger.LogInformation("[RenewalBalanceCheckWorker] Process Renewal Balance Check Async.");
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[RenewalBalanceCheckWorker] Check organization: {0}.", organizationId);
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            
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
            
            //Start process all active renewal
            var billsGrain =
                _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
            var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
            var renewalList = await renewalGrain.GetAllActiveRenewalInfosAsync(organizationId);
            decimal totalPeriodCost = 0;
            var now = DateTime.UtcNow;
            foreach (var renewalDto in renewalList)
            {
                //subscription has not expired and is not yet in the warning period
                if (renewalDto.NextRenewalDate > now.AddDays(_scheduledTaskOptions.RenewalAdvanceWarningDays))
                {
                    continue;
                }

                //Exceeded the maximum days for expiration, clearing assets
                if (renewalDto.NextRenewalDate < now.AddDays(_scheduledTaskOptions.RenewalExpirationMaximumDays))
                {
                    if (renewalDto.ProductType == ProductType.FullPodResource)
                    {
                        if (renewalDto.AppId != _graphQlOptions.BillingIndexerId)
                        {
                            //TODO Obliterate app
                            _logger.LogInformation($"[ProcessRenewalBalanceCheckAsync]App {renewalDto.AppId} is Obliterated.");
                        }
                    }
                }
                
                //subscription is already expired
                if (renewalDto.NextRenewalDate < now.AddDays(-1))
                {
                    _logger.LogInformation($"Subscription {renewalDto.SubscriptionId} found to be expired");
                    //subscription is not charged
                    if (renewalDto.LastChargeDate.AddDays(2) < renewalDto.NextRenewalDate)
                    {
                        _logger.LogInformation($"Subscription {renewalDto.SubscriptionId} found to be not yet charged");
                        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalDto.ProductId);
                        //Charge for previous billing cycle
                        var latestLockedBill = await billsGrain.GetLatestLockedBillAsync(renewalDto.OrderId);
                        var chargeFee = latestLockedBill.BillingAmount;
                        decimal refundAmount = 0;
                        if (renewalDto.ProductType == ProductType.ApiQueryCount)
                        {
                            //Charge based on usage query count
                            var organizationGuid = Guid.Parse(renewalDto.OrganizationId);
                            var monthTime = DateTime.UtcNow.AddMonths(-1);
                            var monthlyQueryCount =
                                await _apiKeyService.GetMonthQueryCountAsync(organizationGuid, monthTime);
                            Logger.LogInformation(
                                $"[ProcessRenewalBalanceCheckAsync]Api monthly query count:{monthlyQueryCount} time:{monthTime.ToString()}");
                            chargeFee = await billsGrain.CalculateApiQueryMonthlyChargeAmountAsync(monthlyQueryCount);
                            var monthlyFee = renewalDto.ProductNumber * productInfo.MonthlyUnitPrice;
                            refundAmount = monthlyFee - chargeFee;
                        }

                        _logger.LogInformation(
                            $"[ProcessRenewalBalanceCheckAsync] Start process charge for previous billing cycle, chargeFee: {chargeFee} refundAmount: {refundAmount}");
                        var chargeBill = await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
                        {
                            OrganizationId = organizationId,
                            OrderId = renewalDto.OrderId,
                            SubscriptionId = renewalDto.SubscriptionId,
                            ChargeFee = chargeFee,
                            Description = "Auto-check-renewal charge for the existing order.",
                            RefundAmount = refundAmount
                        });
                        var sendChargeTransactionOutput = await _contractProvider.BillingChargeAsync(
                            organizationWalletAddress, chargeFee, 0,
                            chargeBill.BillingId);
                        _logger.LogInformation("[ProcessRenewalBalanceCheckAsync] Send charge transaction " +
                                               sendChargeTransactionOutput.TransactionId +
                                               " of bill " + chargeBill.BillingId);
                        var chargeTransactionId = sendChargeTransactionOutput.TransactionId;
                        // not existed->retry  pending->wait  other->fail
                        int delaySeconds = _transactionPollingOptions.DelaySeconds;
                        var chargeTransactionResult =
                            await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                        var chargeResultQueryRetryTimes = 0;
                        while (chargeTransactionResult.Status == TransactionState.NotExisted &&
                               chargeResultQueryRetryTimes < _transactionPollingOptions.RetryTimes)
                        {
                            chargeResultQueryRetryTimes++;

                            await Task.Delay(delaySeconds);
                            chargeTransactionResult =
                                await QueryTransactionResultAsync(chargeTransactionId, delaySeconds);
                        }

                        var chargeTransactionStatus = chargeTransactionResult.Status == TransactionState.Mined
                            ? TransactionState.Mined
                            : TransactionState.Failed;
                        await billsGrain.UpdateTransactionStatus(chargeBill.BillingId, chargeTransactionStatus);
                        _logger.LogInformation(
                            $"[ProcessRenewalBalanceCheckAsync] After {chargeResultQueryRetryTimes} times retry, get charge transaction {chargeTransactionId} status {chargeTransactionStatus}");
                    }

                    //Process expired subscriptions
                    var ordersGrain =
                        _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
                    await ordersGrain.CancelOrderByIdAsync(renewalDto.OrderId);
                    await renewalGrain.CancelRenewalByIdAsync(renewalDto.SubscriptionId);
                    if (renewalDto.ProductType == ProductType.FullPodResource)
                    {
                        if (renewalDto.AppId != _graphQlOptions.BillingIndexerId)
                        {
                            await _appDeployService.FreezeAppAsync(renewalDto.AppId);
                            _logger.LogInformation($"[ProcessRenewalBalanceCheckAsync]App {renewalDto.AppId} is frozen.");
                        }
                    }

                    if (renewalDto.ProductType == ProductType.ApiQueryCount)
                    {
                        var freeQueryAllowance = await _renewalService.GetUserApiQueryFreeCountAsync(renewalDto.OrganizationId);
                        var organizationGuid = Guid.Parse(renewalDto.OrganizationId);
                        await _apiKeyService.SetQueryLimitAsync(organizationGuid, freeQueryAllowance);
                    }
                }
                
                //subscription has not expired but is already in the warning period
                totalPeriodCost = totalPeriodCost + renewalDto.PeriodicCost;
                // if (renewalDto.AppId == _graphQlOptions.BillingIndexerId)
                // {
                //     var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(renewalDto.AppId));
                //     var appInfo = await appGrain.GetAsync();
                //     if (appInfo.Status == AppStatus.Frozen)
                //     {
                //         await _appDeployService.UnFreezeAppAsync(renewalDto.AppId);
                //     }
                // }
            }
            
            
            if (organizationAccountBalance < totalPeriodCost)
            {
                _logger.LogInformation(
                    $"Organization {organizationName} account balance {organizationAccountBalance} is not enough pay for period cost {totalPeriodCost}");
                //TODO Send email to remind user deposit
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
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