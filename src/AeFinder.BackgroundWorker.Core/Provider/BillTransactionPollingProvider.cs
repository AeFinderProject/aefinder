using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User.Provider;
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
    private readonly IOptionsMonitor<TransactionPollingOptions> _transactionPollingOptions;
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

    public BillTransactionPollingProvider(ILogger<BillTransactionPollingProvider> logger,
        IAeFinderIndexerProvider indexerProvider, IClusterClient clusterClient,
        IContractProvider contractProvider, IOrganizationInformationProvider organizationInformationProvider,
        IAppService appService, IProductService productService, IAppDeployService appDeployService,
        IUserInformationProvider userInformationProvider, IRenewalService renewalService,
        IApiKeyService apiKeyService,IOptionsSnapshot<ContractOptions> contractOptions,
        IOptionsMonitor<TransactionPollingOptions> transactionPollingOptions)
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
        _transactionPollingOptions = transactionPollingOptions;
        _contractOptions = contractOptions.Value;
    }

    public async Task HandleTransactionAsync(string billingId,string organizationId)
    {
        var billTransactionResult =
            await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId, null, billingId);
        
        // var times = 0;
        // while ((billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
        //         billTransactionResult.UserFundRecord.Items == null ||
        //         billTransactionResult.UserFundRecord.Items.Count == 0) &&
        //        times < _transactionPollingOptions.CurrentValue.RetryTimes)
        // {
        //     times++;
        //     await Task.Delay(_transactionPollingOptions.CurrentValue.DelaySeconds);
        //     billTransactionResult = await _indexerProvider.GetUserFundRecordAsync(null, billingId);
        // }

        if (billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
            billTransactionResult.UserFundRecord.Items == null ||
            billTransactionResult.UserFundRecord.Items.Count == 0)
        {
            //TODO Record bill transactions that could not be retrieved from the Indexer
            
            return;
        }

        //Update bill transaction id & status
        var transactionResultDto = billTransactionResult.UserFundRecord.Items[0];
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
                _logger.LogInformation($"Order {orderDto.OrderId} status updated paid");
                //Check if there is the same type of product's subscription & order
                // var oldRenewalInfo =
                //     await renewalGrain.GetRenewalInfoByProductTypeAsync(orderDto.ProductType, orderDto.AppId);
                
                //Check the order subscription is existed or Create new subscription
                var renewalInfo = await renewalGrain.GetRenewalInfoByOrderIdAsync(orderDto.OrderId);
                if (renewalInfo == null)
                {
                    await renewalGrain.CreateAsync(new CreateRenewalDto()
                    {
                        OrganizationId = orderDto.OrganizationId,
                        OrderId = orderDto.OrderId,
                        UserId = orderDto.UserId,
                        AppId = orderDto.AppId,
                        ProductId = orderDto.ProductId,
                        ProductNumber = orderDto.ProductNumber,
                        RenewalPeriod = orderDto.RenewalPeriod
                    });
                }

                //Find old order pending bill, call contract to charge bill
                var pendingBills = await billsGrain.GetPendingChargeBillsAsync();
                if (pendingBills != null)
                {
                    foreach (var oldOrderChargeBill in pendingBills)
                    {
                        var oldOrderInfo = await ordersGrain.GetOrderByIdAsync(oldOrderChargeBill.OrderId);
                        if (oldOrderInfo.ProductType != orderDto.ProductType)
                        {
                            continue;
                        }

                        _logger.LogInformation(
                            $"Find pending {oldOrderInfo.ProductType.ToString()} charge bill {oldOrderChargeBill.BillingId}");
                        //Stop old subscription, cancel old order
                        await ordersGrain.CancelOrderByIdAsync(oldOrderChargeBill.OrderId);
                            
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
}