using AeFinder.Billings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class BillingEventHandler :
    IDistributedEventHandler<BillingChangedEto>,
    ITransientDependency
{
    private readonly IBillingService _billingService;

    public BillingEventHandler(IBillingService billingService)
    {
        _billingService = billingService;
    }

    public async Task HandleEventAsync(BillingChangedEto eventData)
    {
        await _billingService.AddOrUpdateIndexAsync(eventData);
    }
}