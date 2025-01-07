using AeFinder.Assets;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using AeFinder.Orders;
using Volo.Abp;
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

        var endTime = State.OrderTime.AddMonths(1).ToMonthDate();
        var orderCost = await _orderCostProvider.CalculateCostAsync(input.Details, State.OrderTime, endTime);

        foreach (var detail in orderCost.Details)
        {
            if (detail.OriginalAsset != null)
            {
                if (detail.OriginalAsset.IsLocked || 
                    detail.OriginalAsset.Status == AssetStatus.Pending ||
                    detail.OriginalAsset.Status == AssetStatus.Released)
                {
                    throw new UserFriendlyException("Unable to repeat orders.");
                }

                if (detail.OriginalAsset.OrganizationId != organizationId)
                {
                    throw new UserFriendlyException("No permission.");
                }
            }
        }

        _objectMapper.Map<OrderCost, OrderState>(orderCost, State);

        if (State.ActualAmount == 0)
        {
            State.Status = OrderStatus.Paid;
        }
        else
        {
            State.Status = OrderStatus.Unpaid;
        }
        
        await WriteStateAsync();
        await PublishOrderStatusChangedEventAsync();

        return State;
    }

    public async Task<OrderState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }
    
    public async Task PayAsync(PaymentType paymentType)
    {
        await ReadStateAsync();
        
        if (State.Id == Guid.Empty)
        {
            throw new EntityNotFoundException();
        }
        
        if (State.Status != OrderStatus.Unpaid && State.Status != OrderStatus.PayFailed)
        {
            throw new UserFriendlyException("Invalid status.");
        }
        
        State.Status = OrderStatus.Confirming;
        State.PaymentType = paymentType;
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

        if (State.Status != OrderStatus.Confirming)
        {
            throw new UserFriendlyException("Invalid status.");
        }

        State.Status = OrderStatus.Paid;
        State.TransactionId = transactionId;
        State.PaymentTime = paymentTime;
        await WriteStateAsync();
        
        await PublishOrderStatusChangedEventAsync();
    }
    
    public async Task PaymentFailedAsync()
    {
        await ReadStateAsync();
        
        if (State.Id == Guid.Empty)
        {
            throw new EntityNotFoundException();
        }
        
        if (State.Status != OrderStatus.Confirming)
        {
            throw new UserFriendlyException("Invalid status.");
        }
        
        State.Status = OrderStatus.PayFailed;
        await WriteStateAsync();
        
        await PublishOrderStatusChangedEventAsync();
    }
    
    public async Task CancelAsync()
    {
        await ReadStateAsync();
        
        if (State.Id == Guid.Empty)
        {
            throw new EntityNotFoundException();
        }
        
        if (State.Status != OrderStatus.Unpaid && State.Status != OrderStatus.PayFailed)
        {
            throw new UserFriendlyException("Invalid status.");
        }
        
        State.Status = OrderStatus.Canceled;
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