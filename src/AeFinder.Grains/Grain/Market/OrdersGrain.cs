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
    
    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
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
        orderState.OrderAmount = orderState.UnitPrice * dto.ProductNumber;
        orderState.OrderStatus = OrderStatus.PendingPayment;
        orderState.EnableAutoRenewal = true;

        if (this.State == null || this.State.Count == 0)
        {
            State = new List<OrderState>();
            State.Add(orderState);
            await WriteStateAsync();
            return _objectMapper.Map<OrderState, OrderDto>(orderState);
        }
        
        State.Add(orderState);
        await WriteStateAsync();
        return _objectMapper.Map<OrderState, OrderDto>(orderState);
    }
    
    private string GenerateId(CreateOrderDto dto)
    {
        string input =
            $"{dto.UserId}-{dto.ProductId}{dto.ProductNumber}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

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
        var billsGrain = GrainFactory.GetGrain<IBillsGrain>(
            GrainIdHelper.GenerateBillsGrainId(Guid.Parse(orderState.OrganizationId)));
        var subscriptionId = string.Empty;
        if (orderState.ProductType == ProductType.ApiQueryCount)
        {
            var apiQueryCountRenewal = await renewalGrain.GetApiQueryCountRenewalInfoAsync(orderState.OrganizationId,
                orderState.UserId, orderState.ProductId);
            subscriptionId = apiQueryCountRenewal.SubscriptionId;
        }

        if (orderState.ProductType == ProductType.FullPodResource)
        {
            var podResourceRenewal = await renewalGrain.GetPodResourceRenewalInfoAsync(orderState.OrganizationId,
                orderState.UserId, orderState.AppId, orderState.ProductId);
            subscriptionId = podResourceRenewal.SubscriptionId;
        }
        orderState.OrderStatus = OrderStatus.Canceled;
        await WriteStateAsync();
        
        await renewalGrain.CancelRenewalByIdAsync(subscriptionId);
        
        
        // //Create charge bill
        // var bill = await billsGrain.CreateChargeBillAsync(orderState.OrganizationId, subscriptionId,
        //     "Order canceled");
        
        // return bill;
    }
}