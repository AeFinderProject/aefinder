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
using AeFinder.Orders;

namespace AeFinder.Assets;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AssetService : AeFinderAppService, IAssetService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<MerchandiseIndex, Guid> _merchandiseIndexRepository;
    private readonly IEntityMappingRepository<AssetIndex, Guid> _assetIndexRepository;

    public AssetService(IClusterClient clusterClient,
        IEntityMappingRepository<MerchandiseIndex, Guid> merchandiseIndexRepository,
        IEntityMappingRepository<AssetIndex, Guid> assetIndexRepository)
    {
        _clusterClient = clusterClient;
        _merchandiseIndexRepository = merchandiseIndexRepository;
        _assetIndexRepository = assetIndexRepository;
    }
    
    public async Task AddOrUpdateIndexAsync(AssetChangedEto input)
    {
        var index = ObjectMapper.Map<AssetChangedEto, AssetIndex>(input);
        var merchandise = await _merchandiseIndexRepository.GetAsync(input.MerchandiseId);
        index.Merchandise = merchandise;
        await _assetIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<PagedResultDto<AssetDto>> GetListsAsync(Guid organizationId, GetAssetInput input)
    {
        var queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
            o.OrganizationId == organizationId && (o.Status == AssetStatus.Unused || o.Status == AssetStatus.Using));
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
        var indices = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<AssetDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<AssetIndex>, List<AssetDto>>(indices)
        };
    }

    public async Task HandlePaidAssetAsync(OrderChangedEto input)
    {
        foreach (var orderDetail in input.Details)
        {
            if (orderDetail.Merchandise != null)
            {
                var originalGrain = _clusterClient.GetGrain<IAssetGrain>(orderDetail.OriginalAsset.Id);
                if (orderDetail.Merchandise.Type == MerchandiseType.ApiQuery)
                {
                    await originalGrain.SuspendAsync();
                }
                else
                {
                    await originalGrain.ReleaseAsync(input.OrderTime);
                }
            }

            var newAssetId = GuidGenerator.Create();
            var newAssetGrain = _clusterClient.GetGrain<IAssetGrain>(newAssetId);
            await newAssetGrain.CreateAssetAsync(newAssetId, input.OrganizationId, new CreateAssetInput
            {
                
            });

            if (input.ExtraData.TryGetValue("AppId", out var appId))
            {
                await newAssetGrain.RelateAppAsync(appId);
            }

            
        }
    }

    public async Task PayAsync(Guid id, decimal paidAmount)
    {
        var grain = _clusterClient.GetGrain<IAssetGrain>(id);
        await grain.PayAsync(paidAmount);
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
}