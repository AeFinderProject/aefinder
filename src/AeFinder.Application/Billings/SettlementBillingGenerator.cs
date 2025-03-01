using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Assets;
using AeFinder.Grains;
using AeFinder.Grains.State.Assets;
using AeFinder.Grains.State.Billings;
using AeFinder.Grains.State.Merchandises;
using AeFinder.Merchandises;
using AElf.EntityMapping.Repositories;
using Remotion.Linq.Clauses;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace AeFinder.Billings;

public class SettlementBillingGenerator : IBillingGenerator
{
    public BillingType BillingType { get; } = BillingType.Settlement;

    private readonly IEntityMappingRepository<AssetIndex, Guid> _assetIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IEnumerable<IResourceUsageProvider> _resourceUsageProviders;

    public SettlementBillingGenerator(IEntityMappingRepository<AssetIndex, Guid> assetIndexRepository,
        IObjectMapper objectMapper, IGuidGenerator guidGenerator, IClock clock,
        IEnumerable<IResourceUsageProvider> resourceUsageProviders)
    {
        _assetIndexRepository = assetIndexRepository;
        _objectMapper = objectMapper;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _resourceUsageProviders = resourceUsageProviders.ToList();
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
                    var assetBeginTime = asset.StartTime < beginTime ? beginTime : asset.StartTime;
                    var assetEndTime = asset.EndTime > endTime ? endTime : asset.EndTime;
                    billingDetail.Quantity =
                        asset.Status == (int)AssetStatus.Unused
                            ? 0
                            : (long)Math.Ceiling((assetEndTime - assetBeginTime).TotalHours);
                    break;
            }

            billingDetail.PaidAmount =
                (billingDetail.Quantity * billingDetail.Replicas - asset.FreeQuantity * asset.FreeReplicas) *
                asset.Merchandise.Price;
            if (billingDetail.PaidAmount < 0)
            {
                billingDetail.PaidAmount = 0;
            }

            billingDetail.RefundAmount = asset.PaidAmount - billingDetail.PaidAmount;

            if (billingDetail.PaidAmount > 0 || billingDetail.RefundAmount > 0)
            {
                billing.Details.Add(billingDetail);
                billing.RefundAmount += billingDetail.RefundAmount;
                billing.PaidAmount += billingDetail.PaidAmount;
            }
        }

        billing = await ProcessTimesSettlementBillingAsync(billing);

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
                ((o.Status == (int)AssetStatus.Unused && o.CreateTime < endTime) ||
                 ((o.Status == (int)AssetStatus.Using || o.Status == (int)AssetStatus.Released) && o.CreateTime < endTime && o.StartTime >= beginTime) ||
                 (o.StartTime < endTime && o.EndTime >= beginTime)))
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
                    ((o.Status == (int)AssetStatus.Unused && o.CreateTime < endTime) ||
                     ((o.Status == (int)AssetStatus.Using || o.Status == (int)AssetStatus.Released) && o.CreateTime < endTime && o.StartTime >= beginTime) ||
                     (o.StartTime < endTime && o.EndTime >= beginTime)))
                .OrderBy(o => o.Merchandise.Type)
                .OrderBy(o => o.StartTime)
                .Skip(skip).Take(maxCount);
            assets = queryable.ToList();
            
            result.AddRange(assets);
        }

        return result;
    }

    private async Task<BillingState> ProcessTimesSettlementBillingAsync(BillingState billing)
    {
        billing.Details.Reverse();
        
        var details = billing.Details;

        billing.Details = new();

        BillingDetailState timeBillingDetail = null;
        decimal assetPaidAmount = 0M;
        foreach (var detail in details)
        {
            if (detail.Merchandise.ChargeType != ChargeType.Time)
            {
                billing.Details.Add(detail);
            }
            else
            {
                if (timeBillingDetail != null && timeBillingDetail.Merchandise.Type != detail.Merchandise.Type)
                {
                    var refundAmount = assetPaidAmount - timeBillingDetail.PaidAmount;
                    
                    timeBillingDetail.Asset.PaidAmount = assetPaidAmount;
                    timeBillingDetail.RefundAmount = refundAmount;
            
                    billing.PaidAmount += timeBillingDetail.PaidAmount;
                    billing.RefundAmount = billing.RefundAmount - assetPaidAmount + refundAmount;
                    
                    billing.Details.Add(timeBillingDetail);

                    timeBillingDetail = null;
                    assetPaidAmount = 0;
                }
            
                if (timeBillingDetail == null)
                {
                    timeBillingDetail = detail;
                    timeBillingDetail.Asset.StartTime = billing.BeginTime;
                    timeBillingDetail.Asset.EndTime = billing.EndTime;
                    
                    var provider = _resourceUsageProviders.First(o => o.MerchandiseType == detail.Merchandise.Type);
                    var usage = await provider.GetUsageAsync(billing.OrganizationId, billing.BeginTime);
                    timeBillingDetail.Quantity = usage;
            
                    timeBillingDetail.PaidAmount =
                        (detail.Quantity * detail.Replicas - detail.Asset.FreeQuantity * detail.Asset.FreeReplicas) *
                        detail.Merchandise.Price;
                    if (timeBillingDetail.PaidAmount < 0)
                    {
                        timeBillingDetail.PaidAmount = 0;
                    }
                }

                assetPaidAmount += detail.Asset.PaidAmount;
            }
        }

        if (timeBillingDetail != null)
        {
            var refundAmount = assetPaidAmount - timeBillingDetail.PaidAmount;

            timeBillingDetail.Asset.PaidAmount = assetPaidAmount;
            timeBillingDetail.RefundAmount = refundAmount;

            billing.PaidAmount += timeBillingDetail.PaidAmount;
            billing.RefundAmount = billing.RefundAmount - assetPaidAmount + refundAmount;

            billing.Details.Add(timeBillingDetail);
        }

        billing.Details.Reverse();
        return billing;
    }
}