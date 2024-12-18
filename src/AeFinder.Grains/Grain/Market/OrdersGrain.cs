using System.Security.Cryptography;
using System.Text;
using AeFinder.Grains.State.Market;
using AeFinder.Market;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Market;

public class OrdersGrain : AeFinderGrain<List<OrderState>>, IOrdersGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public OrdersGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
    
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        await ReadStateAsync();
        
        var orderState = _objectMapper.Map<CreateOrderDto, OrderState>(dto);
        orderState.OrderId = GenerateId(dto);
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(dto.ProductId);
        orderState.ProductType = productInfo.ProductType;
        orderState.ProductName = productInfo.ProductName;
        orderState.UnitPrice = productInfo.MonthlyUnitPrice;
        orderState.OrderDate = DateTime.UtcNow;
        switch (dto.PeriodMonths)
        {
            case 1:
            {
                orderState.RenewalPeriod = RenewalPeriod.OneMonth;
                orderState.OrderAmount = orderState.UnitPrice * dto.ProductNumber * 1;
                break;
            }
            case 3:
            {
                orderState.RenewalPeriod = RenewalPeriod.ThreeMonth;
                orderState.OrderAmount = orderState.UnitPrice * dto.ProductNumber * 3;
                break;
            }
            case 6:
            {
                orderState.RenewalPeriod = RenewalPeriod.SixMonth;
                orderState.OrderAmount = orderState.UnitPrice * dto.ProductNumber * 6;
                break;
            }
            default:
            {
                orderState.RenewalPeriod = RenewalPeriod.OneMonth;
                orderState.OrderAmount = orderState.UnitPrice * dto.ProductNumber * 1;
                break;
            }
        }
        
        orderState.OrderStatus = OrderStatus.PendingPayment;
        orderState.EnableAutoRenewal = true;

        if (this.State == null || this.State.Count == 0)
        {
            State = new List<OrderState>();
        }
        
        State.Add(orderState);
        await WriteStateAsync();
        return _objectMapper.Map<OrderState, OrderDto>(orderState);
    }
    
    private string GenerateId(CreateOrderDto dto)
    {
        string input =
            $"{dto.UserId}-{dto.ProductId}-{dto.ProductNumber}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }

    public async Task<OrderDto> GetOrderByIdAsync(string id)
    {
        var order = State.FirstOrDefault(o => o.OrderId == id);
        var orderInfo = _objectMapper.Map<OrderState, OrderDto>(order);
        return orderInfo;
    }

    public async Task CancelOrderByIdAsync(string id)
    {
        await ReadStateAsync();
        var orderState = State.FirstOrDefault(o => o.OrderId == id);
        var renewalGrain =
            GrainFactory.GetGrain<IRenewalGrain>(GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(orderState.OrganizationId)));
        var subscriptionId = string.Empty;
        if (orderState.ProductType == ProductType.ApiQueryCount)
        {
            var apiQueryCountRenewal = await renewalGrain.GetApiQueryCountRenewalInfoAsync(orderState.OrganizationId,
                orderState.ProductId);
            subscriptionId = apiQueryCountRenewal.SubscriptionId;
        }

        if (orderState.ProductType == ProductType.FullPodResource)
        {
            var podResourceRenewal = await renewalGrain.GetPodResourceRenewalInfoAsync(orderState.OrganizationId,
                orderState.AppId, orderState.ProductId);
            subscriptionId = podResourceRenewal.SubscriptionId;
        }
        orderState.OrderStatus = OrderStatus.Canceled;
        await WriteStateAsync();
        
        await renewalGrain.CancelRenewalByIdAsync(subscriptionId);
    }

    public async Task<OrderDto> GetLatestApiQueryCountOrderAsync(string organizationId)
    {
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        
        var oldUserOrderStates = State.Where(o =>
            o.OrganizationId == organizationId && o.ProductType == ProductType.ApiQueryCount &&
            o.OrderStatus != OrderStatus.Canceled).OrderByDescending(o=>o.OrderDate).ToList();

        foreach (var orderState in oldUserOrderStates)
        {
            var productInfo = await productsGrain.GetProductInfoByIdAsync(orderState.ProductId);
            if (productInfo.MonthlyUnitPrice == 0)
            {
                continue;
            }
            return _objectMapper.Map<OrderState, OrderDto>(orderState);
        }
        return null;
    }

    public async Task<OrderDto> GetLatestPodResourceOrderAsync(string organizationId, string appId)
    {
        var oldUserOrderState = State.FirstOrDefault(o =>
            o.OrganizationId == organizationId && o.ProductType == ProductType.FullPodResource &&
            o.AppId == appId && o.OrderStatus != OrderStatus.Canceled);
        if (oldUserOrderState == null)
        {
            return null;
        }
        return _objectMapper.Map<OrderState, OrderDto>(oldUserOrderState);
    }

    public async Task UpdateOrderStatusAsync(string orderId, OrderStatus orderStatus)
    {
        await ReadStateAsync();
        var orderState = State.FirstOrDefault(o => o.OrderId == orderId);
        orderState.OrderStatus = orderStatus;
        await WriteStateAsync();
    }
}