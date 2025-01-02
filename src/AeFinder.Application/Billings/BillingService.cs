using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.Billings;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp.Application.Dtos;
using System.Linq;

namespace AeFinder.Billings;

public class BillingService : AeFinderAppService, IBillingService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<BillingIndex, Guid> _billingIndexRepository;

    public BillingService(IEntityMappingRepository<BillingIndex, Guid> billingIndexRepository,
        IClusterClient clusterClient)
    {
        _billingIndexRepository = billingIndexRepository;
        _clusterClient = clusterClient;
    }

    public async Task AddOrUpdateIndexAsync(BillingChangedEto eto)
    {
        var index = ObjectMapper.Map<BillingChangedEto, BillingIndex>(eto);
        await _billingIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task<BillingDto> GetAsync(Guid organizationId, Guid id)
    {
        var queryable = await _billingIndexRepository.GetQueryableAsync();
        var order = queryable.FirstOrDefault(o => o.Id == id && o.OrganizationId == organizationId);
        return ObjectMapper.Map<BillingIndex, BillingDto>(order);
    }

    public async Task<PagedResultDto<BillingDto>> GetListAsync(Guid organizationId, GetBillingInput input)
    {
        var queryable = await _billingIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId);
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

    public Task<BillingDto> CreateAsync(Guid organizationId, BillingType type, DateTime dateTime)
    {
        throw new NotImplementedException();
    }

    public async Task PayAsync(Guid id, string transactionId, DateTime paymentTime)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(id);
        await billingGrain.PayAsync(transactionId, paymentTime);
    }

    public async Task ConfirmPaymentAsync(Guid id)
    {
        var billingGrain = _clusterClient.GetGrain<IBillingGrain>(id);
        await billingGrain.ConfirmPaymentAsync();
    }
}