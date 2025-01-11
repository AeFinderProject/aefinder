using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Orders;
using AeFinder.Merchandises;
using AeFinder.Orders;
using AElf.EntityMapping.Repositories;
using MongoDB.Driver.Linq;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AeFinder.Assets;

public class AssetServiceTests : AeFinderApplicationTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly IAssetService _assetService;
    private readonly IObjectMapper _objectMapper;
    private readonly IMerchandiseService _merchandiseService;
    private readonly IEntityMappingRepository<AssetIndex, Guid> _assetIndexRepository;

    public AssetServiceTests()
    {
        _assetService = GetRequiredService<IAssetService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _merchandiseService = GetRequiredService<IMerchandiseService>();
        _assetIndexRepository = GetRequiredService<IEntityMappingRepository<AssetIndex, Guid>>();
    }

    [Fact]
    public async Task Test()
    {
        var merchandises = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Processor
        });
        
        var assetId = Guid.NewGuid();
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(assetId);
        var asset = await assetGrain.CreateAssetAsync(assetId, AeFinderApplicationTestConsts.OrganizationId,new CreateAssetInput
        {
            MerchandiseId = merchandises.Items.First().Id,
            Quantity = 100,
            Replicas = 1,
            CreateTime = DateTime.UtcNow
        });
        await _assetService.AddOrUpdateIndexAsync(_objectMapper.Map<AssetState, AssetChangedEto>(asset));

        var beginTime = DateTime.UtcNow.ToMonthDate();
        var endTime = beginTime.AddMonths(1);
        
        var queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == AeFinderApplicationTestConsts.OrganizationId &&
            o.StartTime < endTime && o.EndTime >= beginTime);
        var list = queryable.ToList();
        

        queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == AeFinderApplicationTestConsts.OrganizationId &&
            (o.Status == (int)AssetStatus.Unused ||
            (o.StartTime < beginTime.AddMonths(1) &&
            o.EndTime >= beginTime.AddMonths(1))));
        list = queryable.ToList();
        
    }

    [Fact]
    public async Task GetListTest()
    {
        var merchandises = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Processor
        });
        
        var assetId = Guid.NewGuid();
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(assetId);
        var asset = await assetGrain.CreateAssetAsync(assetId, AeFinderApplicationTestConsts.OrganizationId,new CreateAssetInput
        {
            MerchandiseId = merchandises.Items.First().Id,
            Quantity = 100,
            Replicas = 1,
            CreateTime = DateTime.UtcNow
        });

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId("appid"));
        await appGrain.CreateAsync(new CreateAppDto
        {
            AppId = "appid",
            AppName = "App",
            OrganizationId = AeFinderApplicationConsts.DefaultAssetExpiration.ToString("N")
        });
        
        await _assetService.RelateAppAsync(AeFinderApplicationTestConsts.OrganizationId, new RelateAppInput
        {
            AppId = "appid",
            AssetIds = new List<Guid> { assetId }
        });
        asset = await assetGrain.GetAsync();

        await _assetService.AddOrUpdateIndexAsync(_objectMapper.Map<AssetState, AssetChangedEto>(asset));

        var list = await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput());
        list.Items.Count.ShouldBe(2);
        
        list = await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput
        {
            AppId = "appid"
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].Merchandise.Id.ShouldBe(merchandises.Items.First().Id);
        
        list = await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput
        {
            Category = MerchandiseCategory.Resource
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].Merchandise.Id.ShouldBe(merchandises.Items.First().Id);
        
        list = await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput
        {
            Type = MerchandiseType.Processor
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].Merchandise.Id.ShouldBe(merchandises.Items.First().Id);
        
        list = await _assetService.GetListAsync(AeFinderApplicationTestConsts.OrganizationId, new GetAssetInput
        {
            IsFree = true
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].Merchandise.Type.ShouldBe(MerchandiseType.ApiQuery);
    }

    [Fact]
    public async Task AssetTest()
    {
        var merchandise = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Storage
        });
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
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
                { AeFinderApplicationConsts.RelateAppExtraDataKey, "AppId" }
            }
        };

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        await orderGrain.CreateAsync(orderId, AeFinderApplicationTestConsts.OrganizationId, userId, input);
        await orderGrain.PayAsync(PaymentType.Wallet);
        await orderGrain.ConfirmPaymentAsync("txid", DateTime.UtcNow);
        var order = await orderGrain.GetAsync();

        var result = await _assetService.ChangeAssetAsync(_objectMapper.Map<OrderState, OrderStatusChangedEto>(order));
        result.Count.ShouldBe(1);
        
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(result[0]);
        var asset = await assetGrain.GetAsync();
        asset.AppId.ShouldBe("AppId");
        asset.Status.ShouldBe(AssetStatus.Unused);
        asset.IsLocked.ShouldBe(false);

        await _assetService.PayAsync(asset.Id, 1000);
        asset = await assetGrain.GetAsync();
        asset.PaidAmount.ShouldBe(1000);

        var time = DateTime.UtcNow;
        await _assetService.StartUsingAssetAsync(asset.Id, time);
        asset = await assetGrain.GetAsync();
        asset.Status.ShouldBe(AssetStatus.Using);
        
        await _assetService.ReleaseAssetAsync(asset.Id, time);
        asset = await assetGrain.GetAsync();
        asset.Status.ShouldBe(AssetStatus.Released);
        
        await _assetService.LockAsync(asset.Id, true);
        asset = await assetGrain.GetAsync();
        asset.IsLocked.ShouldBe(true);
        
        await _assetService.LockAsync(asset.Id, false);
        asset = await assetGrain.GetAsync();
        asset.IsLocked.ShouldBe(false);
    }
    
    [Fact]
    public async Task ChangeAssetTest()
    {
        var merchandise = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.Storage
        });
        
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
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
                { AeFinderApplicationConsts.RelateAppExtraDataKey, "AppId" }
            }
        };

        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        await orderGrain.CreateAsync(orderId, AeFinderApplicationTestConsts.OrganizationId, userId, input);
        await orderGrain.PayAsync(PaymentType.Wallet);
        await orderGrain.ConfirmPaymentAsync("txid", DateTime.UtcNow);
        var order = await orderGrain.GetAsync();

        var result = await _assetService.ChangeAssetAsync(_objectMapper.Map<OrderState, OrderStatusChangedEto>(order));
        result.Count.ShouldBe(1);
        
        var originalAssetId = result[0];
        
        orderId = Guid.NewGuid(); 
        input = new CreateOrderInput
        {
            Details = new List<CreateOrderDetail>
            {
                new CreateOrderDetail
                {
                    OriginalAssetId = originalAssetId,
                    Quantity = 10000,
                    Replicas = 1,
                    MerchandiseId = merchandise.Items.First().Id
                }
            },
            ExtraData = new Dictionary<string, string>
            {
                { AeFinderApplicationConsts.RelateAppExtraDataKey, "AppId" }
            }
        };

        orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        await orderGrain.CreateAsync(orderId, AeFinderApplicationTestConsts.OrganizationId, userId, input);
        await orderGrain.PayAsync(PaymentType.Wallet);
        await orderGrain.ConfirmPaymentAsync("txid", DateTime.UtcNow);
        order = await orderGrain.GetAsync();
        
        result = await _assetService.ChangeAssetAsync(_objectMapper.Map<OrderState, OrderStatusChangedEto>(order));
        result.Count.ShouldBe(1);
        
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(originalAssetId);
        var asset = await assetGrain.GetAsync();
        asset.Status.ShouldBe(AssetStatus.Pending);
    }
}