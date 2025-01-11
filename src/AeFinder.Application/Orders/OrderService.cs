using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Timing;
using System.Linq;

namespace AeFinder.Orders;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class OrderService : AeFinderAppService, IOrderService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<OrderIndex, Guid> _orderIndexRepository;
    private readonly IOrderCostProvider _orderCostProvider;
    private readonly IClock _clock;

    public OrderService(IClusterClient clusterClient, IEntityMappingRepository<OrderIndex, Guid> orderIndexRepository,
        IOrderCostProvider orderCostProvider, IClock clock)
    {
        _clusterClient = clusterClient;
        _orderIndexRepository = orderIndexRepository;
        _orderCostProvider = orderCostProvider;
        _clock = clock;
    }

    public async Task AddOrUpdateIndexAsync(OrderChangedEto eto)
    {
        var index = ObjectMapper.Map<OrderChangedEto, OrderIndex>(eto);
        await _orderIndexRepository.AddOrUpdateAsync(index);
    }

    public async Task UpdateIndexAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await grain.GetAsync();
        var eto = ObjectMapper.Map<OrderState, OrderChangedEto>(order);
        await AddOrUpdateIndexAsync(eto);
    }

    public async Task<PagedResultDto<OrderDto>> GetListAsync(Guid organizationId, GetOrderListInput input)
    {
        var queryable = await _orderIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.OrganizationId == organizationId);
        if (input.BeginTime.HasValue)
        {
            queryable = queryable.Where(o => o.OrderTime >= input.BeginTime.Value);
        }
        
        if (input.EndTime.HasValue)
        {
            queryable = queryable.Where(o => o.OrderTime <= input.EndTime.Value);
        }
        
        if (input.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == (int)input.Status);
        }

        var totalCount = queryable.Count();

        if (input.Sort == OrderSortType.OrderTimeAsc)
        {
            queryable = queryable.OrderBy(o => o.OrderTime);
        }
        else
        {
            queryable = queryable.OrderByDescending(o => o.OrderTime);
        }

        var indices = queryable.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<OrderDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<OrderIndex>, List<OrderDto>>(indices)
        };
    }

    public async Task<OrderDto> GetAsync(Guid organizationId, Guid id)
    {
        var queryable = await _orderIndexRepository.GetQueryableAsync();
        var order = queryable.FirstOrDefault(o => o.Id == id && o.OrganizationId == organizationId);
        return ObjectMapper.Map<OrderIndex, OrderDto>(order);
    }

    public async Task<OrderDto> CreateAsync(Guid organizationId, Guid userId, CreateOrderInput input)
    {
        var id = GuidGenerator.Create();
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await orderGrain.CreateAsync(id, organizationId, userId, input);
        return ObjectMapper.Map<OrderState, OrderDto>(order);
    }

    public async Task<OrderDto> CalculateCostAsync(CreateOrderInput input)
    {
        var orderTime = _clock.Now;
        var endTime = orderTime.AddMonths(1).ToMonthDate();

        var cost = await _orderCostProvider.CalculateCostAsync(input.Details, orderTime, endTime);
        return ObjectMapper.Map<OrderCost, OrderDto>(cost);
    }
    
    public async Task PayAsync(Guid organizationId, Guid id, PayInput input)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await orderGrain.GetAsync();
        if (order.OrganizationId != organizationId)
        {
            throw new UserFriendlyException("No permission.");
        }

        await orderGrain.PayAsync(input.PaymentType);
    }

    public async Task ConfirmPaymentAsync(Guid organizationId, Guid id, string transactionId, DateTime paymentTime)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await orderGrain.GetAsync();
        if (order.OrganizationId != organizationId)
        {
            throw new UserFriendlyException("No permission.");
        }
        await orderGrain.ConfirmPaymentAsync(transactionId, paymentTime);
    }
    
    public async Task PaymentFailedAsync(Guid organizationId, Guid id)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await orderGrain.GetAsync();
        if (order.OrganizationId != organizationId)
        {
            throw new UserFriendlyException("No permission.");
        }

        await orderGrain.PaymentFailedAsync();
    }
    
    public async Task CancelAsync(Guid organizationId, Guid id)
    {
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(id);
        var order = await orderGrain.GetAsync();
        if (order.OrganizationId != organizationId)
        {
            throw new UserFriendlyException("No permission.");
        }

        await orderGrain.CancelAsync();
    }
}