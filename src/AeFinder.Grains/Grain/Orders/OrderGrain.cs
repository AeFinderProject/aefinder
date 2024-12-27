using AeFinder.Assets;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using AeFinder.Orders;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder.Grains.Grain.Orders;

public class OrderGrain : AeFinderGrain<OrderState>, IOrderGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClock _clock;
    private readonly IOrderCostProvider _orderCostProvider;

    public OrderGrain(IObjectMapper objectMapper, IDistributedEventBus distributedEventBus, IClock clock,
        IOrderCostProvider orderCostProvider)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _clock = clock;
        _orderCostProvider = orderCostProvider;
    }

    public async Task<OrderState> CreateAsync(Guid id, Guid organizationId, Guid userId, CreateOrderInput input)
    {
        await ReadStateAsync();
        await BeginChangingStateAsync();
        
        State.Id = id;
        State.OrganizationId = organizationId;
        State.UserId = userId;
        State.ExtraData = input.ExtraData;
        State.OrderTime = _clock.Now;
        State.Status = OrderStatus.Unpaid;

        var endTime = State.OrderTime.AddMonths(1).ToMonthDate();
        var orderCost = await _orderCostProvider.CalculateCostAsync(input.Details, State.OrderTime, endTime);
        // TODO: Verify order

        _objectMapper.Map<OrderCost, OrderState>(orderCost, State);
        
        await WriteStateAsync();
        await PublishOrderStatusChangedEventAsync();

        return State;
    }

    public async Task<OrderState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }

    public async Task UpdateOrderStatusAsync(OrderStatus status)
    {
        await ReadStateAsync();
        if (State.Id == Guid.Empty)
        {
            throw new EntityNotFoundException();
        }

        State.Status = status;
        await WriteStateAsync();
        
        await PublishOrderStatusChangedEventAsync();
    }

    public async Task ConfirmPaymentAsync(string transactionId, DateTime paymentTime)
    {
        await ReadStateAsync();
        
        if (State.Id == Guid.Empty)
        {
            throw new EntityNotFoundException();
        }
        
        State.Status = OrderStatus.Paid;
        State.PaymentTime = paymentTime;
        await WriteStateAsync();
        
        await PublishOrderStatusChangedEventAsync();
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<OrderState, OrderChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
    
    private async Task PublishOrderStatusChangedEventAsync()
    {
        var eventData = _objectMapper.Map<OrderState, OrderStatusChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}