using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User.Provider;
using AElf.Client.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.BackgroundWorker.Core.Provider;

public interface IBillTransactionPollingProvider
{
    Task HandleTransactionAsync(string billingId, string organizationId);
}

public class BillTransactionPollingProvider: IBillTransactionPollingProvider, ISingletonDependency
{
    private readonly ILogger<BillTransactionPollingProvider> _logger;
    private readonly TransactionPollingOptions _transactionPollingOptions;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IContractProvider _contractProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAppService _appService;
    private readonly IProductService _productService;
    private readonly IRenewalService _renewalService;
    private readonly IAppDeployService _appDeployService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IApiKeyService _apiKeyService;
    private readonly ContractOptions _contractOptions;
    private readonly GraphQLOptions _graphQlOptions;

    public BillTransactionPollingProvider(ILogger<BillTransactionPollingProvider> logger,
        IAeFinderIndexerProvider indexerProvider, IClusterClient clusterClient,
        IContractProvider contractProvider, IOrganizationInformationProvider organizationInformationProvider,
        IAppService appService, IProductService productService, IAppDeployService appDeployService,
        IUserInformationProvider userInformationProvider, IRenewalService renewalService,
        IApiKeyService apiKeyService,IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IOptionsSnapshot<TransactionPollingOptions> transactionPollingOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _indexerProvider = indexerProvider;
        _contractProvider = contractProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _userInformationProvider = userInformationProvider;
        _appService = appService;
        _productService = productService;
        _renewalService = renewalService;
        _appDeployService = appDeployService;
        _apiKeyService = apiKeyService;
        _transactionPollingOptions = transactionPollingOptions.Value;
        _contractOptions = contractOptions.Value;
        _graphQlOptions = graphQlOptions.Value;
    }

    public async Task HandleTransactionAsync(string billingId,string organizationId)
    {
        var billTransactionResult =
            await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null, billingId, 0,
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
            _logger.LogError("[HandleTransactionAsync]Get current latest block height failed");
        }
        if (currentLatestBlockHeight <
            (transactionResultDto.Metadata.Block.BlockHeight + _graphQlOptions.SafeBlockCount))
        {
            return;
        }
        
        //Update bill transaction id & status
        _logger.LogInformation(
            $"[HandleTransactionAsync]Get transaction {transactionResultDto.TransactionId} of billing {transactionResultDto.BillingId}");
        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var billDto = await billsGrain.UpdateBillingTransactionInfoAsync(billingId, transactionResultDto.TransactionId,
            transactionResultDto.Amount, transactionResultDto.Address);

        //Handle User Asserts
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var orderDto = await ordersGrain.GetOrderByIdAsync(billDto.OrderId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        switch (billDto.BillingType)
        {
            case BillingType.Lock:
            {
                //Update order lock payment status
                await ordersGrain.UpdateOrderStatusAsync(orderDto.OrderId, OrderStatus.Paid);
                _logger.LogInformation($"[HandleTransactionAsync]Order {orderDto.OrderId} status updated paid");
                
                //Check the order subscription is existed or Create new subscription
                var renewalInfo = await renewalGrain.GetRenewalInfoByOrderIdAsync(orderDto.OrderId);
                if (renewalInfo == null)
                {
                    var newRenewal = await renewalGrain.CreateAsync(new CreateRenewalDto()
                    {
                        OrganizationId = orderDto.OrganizationId,
                        OrderId = orderDto.OrderId,
                        UserId = orderDto.UserId,
                        AppId = orderDto.AppId,
                        ProductId = orderDto.ProductId,
                        ProductNumber = orderDto.ProductNumber,
                        RenewalPeriod = orderDto.RenewalPeriod
                    });
                    _logger.LogInformation($"[HandleTransactionAsync]a new renewal created, subscription id {newRenewal.SubscriptionId} IsActive {newRenewal.IsActive}");
                }

                //Find old order pending bill, call contract to charge bill
                var pendingBills = await billsGrain.GetPendingChargeBillsAsync();
                if (pendingBills != null)
                {
                    foreach (var oldOrderChargeBill in pendingBills)
                    {
                        //Check if there is the same type of product's subscription & order
                        var oldOrderInfo = await ordersGrain.GetOrderByIdAsync(oldOrderChargeBill.OrderId);
                        if (oldOrderInfo.ProductType != orderDto.ProductType)
                        {
                            continue;
                        }

                        _logger.LogInformation(
                            $"[HandleTransactionAsync]Find pending {oldOrderInfo.ProductType.ToString()} charge bill {oldOrderChargeBill.BillingId}");
                        //Stop old subscription, cancel old order
                        await ordersGrain.CancelOrderByIdAsync(oldOrderChargeBill.OrderId);
                        await renewalGrain.CancelRenewalByOrderIdAsync(oldOrderChargeBill.OrderId);
                            
                        //Send charge transaction to contract
                        var userExtensionDto =
                            await _userInformationProvider.GetUserExtensionInfoByIdAsync(
                                Guid.Parse(oldOrderInfo.UserId));
                        var organizationWalletAddress =
                            await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,
                                userExtensionDto.WalletAddress);
                        var sendTransactionOutput = await _contractProvider.BillingChargeAsync(
                            organizationWalletAddress,
                            oldOrderChargeBill.BillingAmount, oldOrderChargeBill.RefundAmount,
                            oldOrderChargeBill.BillingId);
                        _logger.LogInformation("Send charge transaction " + sendTransactionOutput.TransactionId +
                                               " of bill " + oldOrderChargeBill.BillingId);
                        var transactionId = sendTransactionOutput.TransactionId;
                        // not existed->retry  pending->wait  other->fail
                        int delaySeconds = _transactionPollingOptions.DelaySeconds;
                        var transactionResult = await QueryTransactionResultAsync(transactionId,delaySeconds);
                        var times = 0;
                        while (transactionResult.Status == TransactionState.NotExisted &&
                               times < _transactionPollingOptions.RetryTimes)
                        {
                            times++;

                            await Task.Delay(delaySeconds);
                            transactionResult = await QueryTransactionResultAsync(transactionId, delaySeconds);
                        }

                        var status = transactionResult.Status == TransactionState.Mined
                            ? TransactionState.Mined
                            : TransactionState.Failed;
                        await billsGrain.UpdateTransactionStatus(oldOrderChargeBill.BillingId, status);
                        _logger.LogInformation(
                            $"After {times} times retry, get transaction {transactionId} status {status}");
                    }

                }
                
                //Update App pod resource config
                if (orderDto.ProductType == ProductType.FullPodResource)
                {
                    var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
                    var productInfo = await productsGrain.GetProductInfoByIdAsync(orderDto.ProductId);
                    var resourceDto = _productService.ConvertToFullPodResourceDto(productInfo);
                    await _appService.SetAppResourceLimitAsync(orderDto.AppId, new SetAppResourceLimitDto()
                    {
                        AppFullPodLimitCpuCore = resourceDto.Capacity.Cpu,
                        AppFullPodLimitMemory = resourceDto.Capacity.Memory
                    });
                }
                
                //Update Api query limit
                if (orderDto.ProductType == ProductType.ApiQueryCount)
                {
                    var queryAllowance = await _renewalService.GetUserMonthlyApiQueryAllowanceAsync(orderDto.OrganizationId);
                    var organizationGuid = Guid.Parse(orderDto.OrganizationId);
                    await _apiKeyService.SetQueryLimitAsync(organizationGuid, queryAllowance);
                }
                
                break;
            }
            case BillingType.Charge:
            {
                //Check subscription is existed, or throw exception 
                var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(billDto.SubscriptionId);
                if (renewalInfo == null)
                {
                    throw new Exception(
                        $"Failed to find subscription information corresponding to the billing invoice. Billing Id:{billDto.BillingId}");
                }
                //Update Subscription charge date
                await renewalGrain.UpdateLastChargeDateAsync(renewalInfo.SubscriptionId, billDto.BillingDate);
                break;
            }
            case BillingType.LockFrom:
            {
                //Update Subscription next lock date
                await renewalGrain.UpdateRenewalDateToNextPeriodAsync(billDto.SubscriptionId);
                //Check AeIndexer status, if frozen, unfroze it
                var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(billDto.SubscriptionId);
                if (renewalInfo.ProductType == ProductType.FullPodResource)
                {
                    var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(renewalInfo.AppId));
                    var app = await appGrain.GetAsync();
                    if (app.Status == AppStatus.Frozen)
                    {
                        await appGrain.UnFreezeAppAsync();
                        //ReDeploy Indexer
                        await _appDeployService.ReDeployAppAsync(renewalInfo.AppId);
                    }
                }
                break;
            }
        }

    }
    
    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
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