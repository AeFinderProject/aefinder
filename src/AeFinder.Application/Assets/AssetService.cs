using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Merchandises;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using System.Linq;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.State.Assets;
using AeFinder.Orders;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Assets;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AssetService : AeFinderAppService, IAssetService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<MerchandiseIndex, Guid> _merchandiseIndexRepository;
    private readonly IEntityMappingRepository<AssetIndex, Guid> _assetIndexRepository;
    private readonly IDistributedEventBus _distributedEventBus;

    public AssetService(IClusterClient clusterClient,
        IEntityMappingRepository<MerchandiseIndex, Guid> merchandiseIndexRepository,
        IEntityMappingRepository<AssetIndex, Guid> assetIndexRepository, IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _merchandiseIndexRepository = merchandiseIndexRepository;
        _assetIndexRepository = assetIndexRepository;
        _distributedEventBus = distributedEventBus;
    }

    public async Task AddOrUpdateIndexAsync(AssetChangedEto input)
    {
        var index = ObjectMapper.Map<AssetChangedEto, AssetIndex>(input);
        var merchandise = await _merchandiseIndexRepository.GetAsync(input.MerchandiseId);
        index.Merchandise = merchandise;
        await _assetIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<PagedResultDto<AssetDto>> GetListAsync(Guid organizationId, GetAssetInput input)
    {
        var queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == organizationId);
        if (input.IsDeploy)
        {
            queryable = queryable.Where(o => 
                o.Status == (int)AssetStatus.Unused || o.Status == (int)AssetStatus.Using);
        }
        else
        {
            queryable = queryable.Where(o =>
                o.Status == (int)AssetStatus.Unused || o.Status == (int)AssetStatus.Using ||
                o.Status == (int)AssetStatus.Pending);
        }

        if (!input.AppId.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(o => o.AppId == input.AppId);
        }

        if (input.IsFree.HasValue)
        {
            queryable = queryable.Where(o => o.FreeQuantity > 0);
        }

        if (input.Type.HasValue)
        {
            queryable = queryable.Where(o => o.Merchandise.Type == (int)input.Type);
        }

        if (input.Category.HasValue)
        {
            queryable = queryable.Where(o => o.Merchandise.Category == (int)input.Category);
        }

        var totalCount = queryable.Count();
        var indices = queryable.OrderBy(o => o.Id).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<AssetDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<AssetIndex>, List<AssetDto>>(indices)
        };
    }

    public async Task<AssetDto> CreateAsync(Guid organizationId, CreateAssetInput input)
    {
        var assetId = GuidGenerator.Create();
        var assetGrain = _clusterClient.GetGrain<IAssetGrain>(assetId);
        var asset = await assetGrain.CreateAssetAsync(assetId, organizationId, input);
        return ObjectMapper.Map<AssetState, AssetDto>(asset);
    }

    public async Task<List<Guid>> ChangeAssetAsync(OrderStatusChangedEto input)
    {
        var result = new List<Guid>();;
        if (input.Status != OrderStatus.Paid)
        {
            return result;
        }

        input.ExtraData.TryGetValue(AeFinderApplicationConsts.RelateAppExtraDataKey, out var appId);
        var appAssetChangedEto = new AppAssetChangedEto
        {
            AppId = appId
        };
        
        foreach (var orderDetail in input.Details)
        {
            var changedAsset = new ChangedAsset();
            
            if (orderDetail.OriginalAsset != null)
            {
                var originalGrain = _clusterClient.GetGrain<IAssetGrain>(orderDetail.OriginalAsset.Id);
                await originalGrain.SuspendAsync();
                changedAsset.OriginalAsset = orderDetail.OriginalAsset;
            }

            var newAssetId = GuidGenerator.Create();
            var newAssetGrain = _clusterClient.GetGrain<IAssetGrain>(newAssetId);
            var newAssetInput = new CreateAssetInput
            {
                MerchandiseId = orderDetail.Merchandise.Id,
                PaidAmount = orderDetail.ActualAmount,
                Quantity = orderDetail.Quantity,
                Replicas = orderDetail.Replicas,
                CreateTime = input.OrderTime
            };
            if (orderDetail.OriginalAsset != null && orderDetail.OriginalAsset.FreeType == AssetFreeType.Permanent)
            {
                newAssetInput.FreeQuantity = orderDetail.OriginalAsset.FreeQuantity;
                newAssetInput.FreeReplicas = orderDetail.OriginalAsset.FreeReplicas;
                newAssetInput.FreeType = AssetFreeType.Permanent;
            }

            var asset = await newAssetGrain.CreateAssetAsync(newAssetId, input.OrganizationId, newAssetInput);
            result.Add(asset.Id);

            if (!appId.IsNullOrEmpty())
            {
                await newAssetGrain.RelateAppAsync(appId);
                asset = await newAssetGrain.GetAsync();
            }
            
            changedAsset.Asset = ObjectMapper.Map<AssetState, AssetChangedEto>(asset);
            
            appAssetChangedEto.ChangedAssets.Add(changedAsset);
        }

        await _distributedEventBus.PublishAsync(appAssetChangedEto);

        return result;
    }

    public async Task PayAsync(Guid id, decimal paidAmount)
    {
        var grain = _clusterClient.GetGrain<IAssetGrain>(id);
        await grain.PayAsync(paidAmount);
    }

    public async Task RelateAppAsync(Guid organizationId, RelateAppInput input)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(input.AppId));
        var app = await appGrain.GetAsync();
        if (app == null || app.AppId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Invalid App.");
        }

        var appAssetChangedEto = new AppAssetChangedEto
        {
            AppId = input.AppId
        };
        
        foreach (var assetId in input.AssetIds)
        {
            var assetGrain = _clusterClient.GetGrain<IAssetGrain>(assetId);
            var asset = await assetGrain.GetAsync();
            if (asset.OrganizationId != organizationId)
            {
                throw new UserFriendlyException("No permission.");
            }

            await assetGrain.RelateAppAsync(input.AppId);
            
            var changedAsset = new ChangedAsset
            {
                Asset = ObjectMapper.Map<AssetState, AssetChangedEto>(asset)
            };
            appAssetChangedEto.ChangedAssets.Add(changedAsset);
        }
        
        await _distributedEventBus.PublishAsync(appAssetChangedEto);
    }

    public async Task<decimal> CalculateMonthlyCostAsync(Guid organizationId, DateTime dateTime)
    {
        var beginTime = dateTime.ToMonthDate();
        var endTime = beginTime.AddMonths(1);

        var assets = new List<AssetIndex>();
        var queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable
            .Where(o => o.OrganizationId == organizationId && o.StartTime < beginTime && o.EndTime >= beginTime)
            .OrderBy(o => o.Merchandise.Type).OrderBy(o => o.StartTime);
        assets = queryable.ToList();

        var totalAmount = 0M;
        foreach (var asset in assets)
        {
            var quantity = 0L;
            switch ((ChargeType)asset.Merchandise.ChargeType)
            {
                case ChargeType.Hourly:
                    quantity = (long)Math.Ceiling((endTime - beginTime).TotalHours);
                    break;
                case ChargeType.Time:
                    quantity = asset.Quantity;
                    break;
            }

            var amount =
                (quantity * asset.Replicas - asset.FreeQuantity * asset.FreeReplicas) *
                asset.Merchandise.Price;
            if (amount < 0)
            {
                amount = 0;
            }

            totalAmount += amount;
        }
        
        return totalAmount;
    }

    public async Task StartUsingAssetAsync(Guid id, DateTime dateTime)
    {
        var grain = _clusterClient.GetGrain<IAssetGrain>(id);
        await grain.StartUsingAsync(dateTime);
    }

    public async Task ReleaseAssetAsync(Guid id, DateTime dateTime)
    {
        var grain = _clusterClient.GetGrain<IAssetGrain>(id);
        await grain.ReleaseAsync(dateTime);
    }

    public async Task LockAsync(Guid id, bool isLock)
    {
        var grain = _clusterClient.GetGrain<IAssetGrain>(id);
        await grain.LockAsync(isLock);
    }
}