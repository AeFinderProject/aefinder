using AeFinder.BackgroundWorker.Core.Provider;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Market.Eto;
using Hangfire;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class BillCreateHandler: IDistributedEventHandler<BillCreateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly TransactionPollingOptions _transactionPollingOptions;

    public BillCreateHandler(IClusterClient clusterClient, IOptionsSnapshot<TransactionPollingOptions> options)
    {
        _clusterClient = clusterClient;
        _transactionPollingOptions = options.Value;
    }

    public async Task HandleEventAsync(BillCreateEto eventData)
    {
        //Start delayed initiation of background thread polling for indexer bill transactions.
        BackgroundJob.Schedule<IBillTransactionPollingProvider>(provider =>
                provider.HandleTransactionAsync(eventData.BillingId, eventData.OrganizationId),
            TimeSpan.FromSeconds(_transactionPollingOptions.DelaySeconds));
    }
}