using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AeFinder.Orders;

public class OrderServiceTests : AeFinderApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrderService _orderService;
    private readonly IAssetService _assetService;
    private readonly IObjectMapper _objectMapper;
    private readonly IMerchandiseService _merchandiseService;

    public OrderServiceTests()
    {
        _orderService = GetRequiredService<IOrderService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _assetService = GetRequiredService<IAssetService>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _merchandiseService = GetRequiredService<IMerchandiseService>();
    }

    [Fact]
    public async Task Get_Test()
    {
        var assets =
            await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput());
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var input = new CreateOrderInput
        {
            Details = new List<CreateOrderDetail>
            {
                new CreateOrderDetail
                {
                    OriginalAssetId = assets.Items.First().Id,
                    Quantity = 110000,
                    Replicas = 1,
                    MerchandiseId = assets.Items.First().Merchandise.Id
                }
            }
        };
        var order = await orderGrain.CreateAsync(id, AeFinderApplicationTestConsts.OrganizationId, userId, input);

        await _orderService.AddOrUpdateIndexAsync(_objectMapper.Map<OrderState, OrderChangedEto>(order));

        var list = await _orderService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId,
            new GetOrderListInput());
        list.Items.Count.ShouldBe(1);

        var orderDto = await _orderService.GetAsync(AeFinderApplicationTestConsts.OrganizationId, id);
        orderDto.Id.ShouldBe(id);
    }

    [Fact]
    public async Task Order_Test()
    {
        var merchandise = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Storage
        });
        
        var userId = Guid.NewGuid();
        
        var input = new CreateOrderInput
        {
            Details = new List<CreateOrderDetail>
            {
                new CreateOrderDetail
                {
                    Quantity = 10000,
                    Replicas = 1,
                    MerchandiseId = merchandise.Items.First().Id
                }
            },
            ExtraData = new Dictionary<string, string>
            {
                { AeFinderApplicationConsts.RelateAppExtraDataKey, "appid" }
            }
        };

        var order = await _orderService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId, userId, input);
        order.Amount.ShouldBeGreaterThan(0);
        order.Status.ShouldBe(OrderStatus.Unpaid);
        order.PaymentType.ShouldBe(PaymentType.None);
        
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId("appid"));
        var app = await appGrain.GetAsync();
        app.IsLocked.ShouldBeTrue();

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId("appid"));
        var app = await appGrain.GetAsync();
        app.IsLocked.ShouldBeTrue();
        
        await _orderService.PayAsync(AeFinderApplicationTestConsts.OrganizationId, order.Id, new PayInput
        {
            PaymentType = PaymentType.Wallet
        });

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(order.Id);
        var orderState = await orderGrain.GetAsync();
        orderState.Status.ShouldBe(OrderStatus.Confirming);
        orderState.PaymentType.ShouldBe(PaymentType.Wallet);

        var payTime = DateTime.UtcNow;
        await _orderService.ConfirmPaymentAsync(AeFinderApplicationTestConsts.OrganizationId, order.Id, "txid", payTime);
        
        orderState = await orderGrain.GetAsync();
        orderState.Status.ShouldBe(OrderStatus.Paid);
        orderState.TransactionId.ShouldBe("txid");
        orderState.PaymentTime.ShouldBe(payTime);
    }
    
    [Fact]
    public async Task Order_PayFailed_Test()
    {
        var merchandise = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Storage
        });
        
        var userId = Guid.NewGuid();
        
        var input = new CreateOrderInput
        {
            Details = new List<CreateOrderDetail>
            {
                new CreateOrderDetail
                {
                    Quantity = 10000,
                    Replicas = 1,
                    MerchandiseId = merchandise.Items.First().Id
                }
            },
            ExtraData = new Dictionary<string, string>()
        };

        var order = await _orderService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId, userId, input);
        order.Status.ShouldBe(OrderStatus.Unpaid);
        order.PaymentType.ShouldBe(PaymentType.None);

        await _orderService.PayAsync(AeFinderApplicationTestConsts.OrganizationId, order.Id, new PayInput
        {
            PaymentType = PaymentType.Wallet
        });

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(order.Id);
        var orderState = await orderGrain.GetAsync();
        orderState.Status.ShouldBe(OrderStatus.Confirming);
        orderState.PaymentType.ShouldBe(PaymentType.Wallet);

        await _orderService.PaymentFailedAsync(AeFinderApplicationTestConsts.OrganizationId, order.Id);
        
        orderState = await orderGrain.GetAsync();
        orderState.Status.ShouldBe(OrderStatus.PayFailed);
    }
    
    [Fact]
    public async Task Order_Cancel_Test()
    {
        var merchandise = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Storage
        });
        
        var userId = Guid.NewGuid();
        
        var input = new CreateOrderInput
        {
            Details = new List<CreateOrderDetail>
            {
                new CreateOrderDetail
                {
                    Quantity = 10000,
                    Replicas = 1,
                    MerchandiseId = merchandise.Items.First().Id
                }
            },
            ExtraData = new Dictionary<string, string>()
        };

        var order = await _orderService.CreateAsync(AeFinderApplicationTestConsts.OrganizationId, userId, input);
        order.Status.ShouldBe(OrderStatus.Unpaid);
        order.PaymentType.ShouldBe(PaymentType.None);

        await _orderService.CancelAsync(AeFinderApplicationTestConsts.OrganizationId, order.Id);
        
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(order.Id);
        var orderState = await orderGrain.GetAsync();
        orderState.Status.ShouldBe(OrderStatus.Canceled);
    }
}