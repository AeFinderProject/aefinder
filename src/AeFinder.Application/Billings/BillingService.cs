using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.Billings;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp.Application.Dtos;
using System.Linq;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.State.Billings;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.Billings;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BillingService : AeFinderAppService, IBillingService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<BillingIndex, Guid> _billingIndexRepository;
    private readonly IEnumerable<IBillingGenerator> _billingGenerators;

    public BillingService(IEntityMappingRepository<BillingIndex, Guid> billingIndexRepository,
        IClusterClient clusterClient, IEnumerable<IBillingGenerator> billingGenerators)
    {
        _billingIndexRepository = billingIndexRepository;
        _clusterClient = clusterClient;
        _billingGenerators = billingGenerators.ToList();
    }

    public async Task AddOrUpdateIndexAsync(BillingChangedEto eto)
    {
        var index = ObjectMapper.Map<BillingChangedEto, BillingIndex>(eto);
        await _billingIndexRepository.AddOrUpdateAsync(index);
    }
    
    public async Task UpdateIndexAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<IBillingGrain>(id);
        var billing = await grain.GetAsync();
        var eto = ObjectMapper.Map<BillingState, BillingChangedEto>(billing);
        await AddOrUpdateIndexAsync(eto);
    }

    public async Task<BillingDto> GetAsync(Guid? organizationId, Guid id)
    {
        var billing = await _billingIndexRepository.GetAsync(id);
        if (organizationId.HasValue && billing.OrganizationId != organizationId.Value)
        {
            throw new UserFriendlyException("No permission.");
        }

        return ObjectMapper.Map<BillingIndex, BillingDto>(billing);
    }

    public async Task<PagedResultDto<BillingDto>> GetListAsync(Guid? organizationId, GetBillingInput input)
    {
        var queryable = await _billingIndexRepository.GetQueryableAsync();
        if (organizationId.HasValue)
        {
            queryable = queryable.Where(o => o.OrganizationId == organizationId.Value);
        }
        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.BeginTime >= input.BeginTime.Value);
        }

        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.EndTime <= input.EndTime.Value);
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == (int)input.Status);
        }

        if (input.Type.HasValue)
        {
            queryable = queryable.Where(o => o.Type == (int)input.Type);
        }

        var totalCount = queryable.Count();
        if (input.Sort == BillingSortType.BillingTimeAsc)
        {
            queryable = queryable.OrderBy(o => o.BeginTime);
        }
        else
        {
            queryable = queryable.OrderByDescending(o => o.BeginTime);
        }

        var indices = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<BillingDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<BillingIndex>, List<BillingDto>>(indices)
        };
    }

    public async Task<BillingDto> CreateAsync(Guid organizationId, BillingType type, DateTime dateTime)
    {
        var generator = _billingGenerators.First(o => o.BillingType == type);
        var billing = await generator.GenerateBillingAsync(organizationId, dateTime);

        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(billing.Id);
        await billingGrain.CreateAsync(billing);
        return ObjectMapper.Map<BillingState, BillingDto>(billing);
    }

    public async Task PayAsync(Guid id, string transactionId, DateTime paymentTime)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(id);
        await billingGrain.PayAsync(transactionId, paymentTime);
    }

    public async Task ConfirmPaymentAsync(Guid id)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(id);

        var billing = await billingGrain.GetAsync();
        if (billing.Type == BillingType.AdvancePayment)
        {
            foreach (var detail in billing.Details)
            {
                var assetGrain = _clusterClient.GetGrain<IAssetGrain>(detail.Asset.Id);
                await assetGrain.PayAsync(detail.PaidAmount);
            }
        }

        await billingGrain.ConfirmPaymentAsync();
    }
    
    public async Task PaymentFailedAsync(Guid id)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(id);
        await billingGrain.PaymentFailedAsync();
    }
}