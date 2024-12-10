using AeFinder.BackgroundWorker.Options;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
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

    public BillTransactionPollingProvider(ILogger<BillTransactionPollingProvider> logger,
        IAeFinderIndexerProvider indexerProvider,IClusterClient clusterClient,
        IOptionsMonitor<TransactionPollingOptions> transactionPollingOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _indexerProvider = indexerProvider;
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
        switch (billDto.BillingType)
        {
            case BillingType.Lock:
            {
                //Update order lock payment status
                
                //Check the order subscription is existed or Create new subscription
                
                //Check if there is the same type of product's subscription & order
                
                //Stop old subscription
                
                //Find old order bill, call contract to charge & refund bill
                break;
            }
            case BillingType.Charge:
            {
                //Check subscription is existed, or throw exception 
                
                //Update Subscription charge date
                break;
            }
            case BillingType.LockFrom:
            {
                //Update Subscription next lock date
                
                //Check AeIndexer status, if frozen, unfroze it
                break;
            }
        }
        
        
        
        if (billDto.SubscriptionId.IsNullOrEmpty())
        {
            
        }
    }
    
    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }
}