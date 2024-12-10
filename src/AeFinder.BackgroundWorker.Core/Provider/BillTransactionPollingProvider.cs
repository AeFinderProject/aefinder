using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
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

    public BillTransactionPollingProvider(ILogger<BillTransactionPollingProvider> logger,
        IAeFinderIndexerProvider indexerProvider, IClusterClient clusterClient,
        IContractProvider contractProvider, IOrganizationInformationProvider organizationInformationProvider,
        IOptionsMonitor<TransactionPollingOptions> transactionPollingOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _indexerProvider = indexerProvider;
        _contractProvider = contractProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _transactionPollingOptions = transactionPollingOptions;
    }

    public async Task HandleTransactionAsync(string billingId,string organizationId)
    {
        var billTransactionResult = await _indexerProvider.GetUserFundRecordAsync(null, billingId);
        
        var times = 0;
        while ((billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
                billTransactionResult.UserFundRecord.Items == null ||
                billTransactionResult.UserFundRecord.Items.Count == 0) &&
               times < _transactionPollingOptions.CurrentValue.RetryTimes)
        {
            times++;
            await Task.Delay(_transactionPollingOptions.CurrentValue.DelaySeconds);
            billTransactionResult = await _indexerProvider.GetUserFundRecordAsync(null, billingId);
        }

        if (billTransactionResult == null || billTransactionResult.UserFundRecord == null ||
            billTransactionResult.UserFundRecord.Items == null ||
            billTransactionResult.UserFundRecord.Items.Count == 0)
        {
            //TODO Record bill transactions that could not be retrieved from the Indexer
            
            return;
        }

        //Update bill transaction id & status
        var transactionResultDto = billTransactionResult.UserFundRecord.Items[0];
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
                
                //Check if there is the same type of product's subscription & order
                var oldRenewalInfo =
                    await renewalGrain.GetRenewalInfoByProductTypeAsync(orderDto.ProductType, orderDto.AppId,
                        orderDto.UserId);
                
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

                if (oldRenewalInfo == null)
                {
                    break;
                }
                
                //Stop old subscription, cancel old order
                await ordersGrain.CancelOrderByIdAsync(oldRenewalInfo.OrderId);
                
                //Find old order pending bill, call contract to charge bill
                var oldOrderChargeBill = await billsGrain.GetPendingChargeBillByOrderIdAsync(oldRenewalInfo.OrderId);
                if (oldOrderChargeBill != null)
                {
                    //Send charge transaction to contract
                    var organizationWalletAddress =
                        await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId);
                    var sendTransactionOutput = await _contractProvider.BillingChargeAsync(organizationWalletAddress,
                        oldOrderChargeBill.BillingAmount, oldOrderChargeBill.RefundAmount,
                        oldOrderChargeBill.BillingId);
                }
                
                //TODO ReDeploy Indexer?
                
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
                        //TODO ReDeploy Indexer?

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