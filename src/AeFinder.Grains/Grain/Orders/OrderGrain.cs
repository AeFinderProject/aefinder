using AeFinder.Assets;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using AeFinder.Orders;
using Microsoft.Extensions.Logging;
using Serilog.Core;
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
    private readonly IEnumerable<IOrderValidationProvider> _validationProviders;
    private readonly IEnumerable<IOrderHandler> _orderHandlers;
    private readonly ILogger<OrderGrain> _logger;

    public OrderGrain(IObjectMapper objectMapper, IDistributedEventBus distributedEventBus, IClock clock,
        IOrderCostProvider orderCostProvider, IEnumerable<IOrderValidationProvider> validationProviders,
        IEnumerable<IOrderHandler> orderHandlers, ILogger<OrderGrain> logger)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _clock = clock;
        _orderCostProvider = orderCostProvider;
        _validationProviders = validationProviders;
        _orderHandlers = orderHandlers;
        _logger = logger;
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
                if (detail.OriginalAsset.OrganizationId != organizationId)
                {
                    throw new UserFriendlyException("No permission.");
                }
            }
        }
        
        if (orderCost.ActualAmount == 0)
        {
            State.Status = OrderStatus.Paid;
        }
        else
        {
            State.Status = OrderStatus.Unpaid;
        }
        
        _objectMapper.Map<OrderCost, OrderState>(orderCost, State);

        await ValidateBeforeOrderAsync();
        
        await WriteStateAsync();

        await HandleCreatedAsync();
        
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
        State.PaymentTime = _clock.Now;

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

    private async Task<bool> ValidateBeforeOrderAsync()
    {
        foreach (var provider in _validationProviders)
        {
            if (!await provider.ValidateBeforeOrderAsync(State))
            {
                _logger.LogWarning("Validate before order failed: {ProviderTypeName}", provider.GetType().Name);
                return false;
            }
        }

        return true;
    }

    private async Task HandleCreatedAsync()
    {
        foreach (var provider in _orderHandlers)
        {
            await provider.HandleOrderCreatedAsync(State);
        }
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