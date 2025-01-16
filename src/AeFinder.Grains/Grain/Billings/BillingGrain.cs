using AeFinder.Billings;
using AeFinder.Grains.State.Billings;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Billings;

public class BillingGrain : AeFinderGrain<BillingState>, IBillingGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public BillingGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task<BillingState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }

    public async Task CreateAsync(BillingState billing)
    {
        State = billing;
        await WriteStateAsync();
    }

    public async Task PayAsync(string transactionId, DateTime paymentTime)
    {
        await ReadStateAsync();

        State.Status = BillingStatus.Confirming;
        State.TransactionId = transactionId;
        State.PaymentTime = paymentTime;

        await WriteStateAsync();
    }

    public async Task ConfirmPaymentAsync()
    {
        await ReadStateAsync();

        State.Status = BillingStatus.Paid;

        await WriteStateAsync();
    }
    
    public async Task PaymentFailedAsync()
    {
        await ReadStateAsync();

        State.Status = BillingStatus.Failed;

        await WriteStateAsync();
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<BillingState, BillingChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}