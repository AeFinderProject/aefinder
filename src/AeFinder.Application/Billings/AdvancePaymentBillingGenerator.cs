using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Grains;
using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Billings;
using AeFinder.Grains.State.Merchandises;
using AeFinder.Merchandises;
using AElf.EntityMapping.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Timing;

namespace AeFinder.Billings;

public class AdvancePaymentBillingGenerator : IBillingGenerator
{
    public BillingType BillingType { get; } = BillingType.AdvancePayment;

    private readonly IEntityMappingRepository<AssetIndex, Guid> _assetIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public AdvancePaymentBillingGenerator(IEntityMappingRepository<AssetIndex, Guid> assetIndexRepository,
        IObjectMapper objectMapper, IGuidGenerator guidGenerator, IClock clock)
    {
        _assetIndexRepository = assetIndexRepository;
        _objectMapper = objectMapper;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<BillingState> GenerateBillingAsync(Guid organizationId, DateTime dateTime)
    {
        var beginTime = dateTime.ToMonthDate();
        var endTime = beginTime.AddMonths(1);

        var assets = await GetAssetsAsync(organizationId, beginTime, endTime);

        var billing = new BillingState();
        billing.Id = _guidGenerator.Create();
        billing.OrganizationId = organizationId;
        billing.BeginTime = beginTime;
        billing.EndTime = endTime.AddSeconds(-1);
        billing.Type = BillingType;
        billing.Status = BillingStatus.Unpaid;
        billing.CreateTime = _clock.Now;

        foreach (var asset in assets)
        {
            var billingDetail = new BillingDetailState();
            billingDetail.Merchandise = _objectMapper.Map<MerchandiseIndex, MerchandiseState>(asset.Merchandise);
            billingDetail.Asset = _objectMapper.Map<AssetIndex, AssetState>(asset);
            billingDetail.Asset.MerchandiseId = asset.Merchandise.Id;
            billingDetail.Replicas = asset.Replicas;

            switch ((ChargeType)asset.Merchandise.ChargeType)
            {
                case ChargeType.Hourly:
                    var assetEndTime = (asset.EndTime < endTime && asset.Status != (int)AssetStatus.Unused)
                        ? asset.EndTime
                        : endTime;
                    billingDetail.Quantity = (long)Math.Ceiling((assetEndTime - beginTime).TotalHours);
                    break;
                case ChargeType.Time:
                    billingDetail.Quantity = asset.Quantity;
                    break;
            }

            billingDetail.PaidAmount =
                (billingDetail.Quantity * billingDetail.Replicas - asset.FreeQuantity * asset.FreeReplicas) *
                asset.Merchandise.Price;
            if (billingDetail.PaidAmount < 0)
            {
                billingDetail.PaidAmount = 0;
            }

            if (billingDetail.PaidAmount > 0 || billingDetail.RefundAmount > 0)
            {
                billing.Details.Add(billingDetail);
                billing.RefundAmount += billingDetail.RefundAmount;
                billing.PaidAmount += billingDetail.PaidAmount;
            }
        }

        return billing;
    }
    
    private async Task<List<AssetIndex>> GetAssetsAsync(Guid organizationId, DateTime beginTime,DateTime endTime)
    {
        var skip = 0;
        var maxCount = 1000;
        var result = new List<AssetIndex>();
        
        var queryable = await _assetIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o =>
                o.OrganizationId == organizationId &&
                ((o.Status == (int)AssetStatus.Unused && o.CreateTime < beginTime) || 
                 ((o.Status == (int)AssetStatus.Using || o.Status == (int)AssetStatus.Released) && o.CreateTime<beginTime && o.StartTime>=beginTime) ||
                 (o.StartTime < beginTime && o.EndTime >= beginTime)))
            .OrderBy(o => o.Merchandise.Type)
            .OrderBy(o => o.StartTime)
            .Skip(skip).Take(maxCount);
        
        var assets = queryable.ToList();
        result.AddRange(assets);

        while (assets.Count ==maxCount)
        {
            skip += maxCount;
            queryable = queryable.Where(o => 
                    o.OrganizationId == organizationId && 
                    ((o.Status == (int)AssetStatus.Unused && o.CreateTime < beginTime) || 
                     ((o.Status == (int)AssetStatus.Using || o.Status == (int)AssetStatus.Released) && o.CreateTime<beginTime && o.StartTime>=beginTime) ||
                     (o.StartTime < beginTime && o.EndTime >= beginTime)))                
                .OrderBy(o => o.Merchandise.Type)
                .OrderBy(o => o.StartTime)
                .Skip(skip).Take(maxCount);
            assets = queryable.ToList();
            
            result.AddRange(assets);
        }

        return result;
    }
}