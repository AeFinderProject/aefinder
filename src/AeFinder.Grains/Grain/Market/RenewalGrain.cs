using AeFinder.Grains.State.Market;
using AeFinder.Market;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Market;

public class RenewalGrain: AeFinderGrain<List<RenewalState>>, IRenewalGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public RenewalGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<RenewalDto> CreateAsync(CreateRenewalDto dto)
    {
        await ReadStateAsync();
        if (State == null)
        {
            State = new List<RenewalState>();
        }

        var renewalItem = _objectMapper.Map<CreateRenewalDto, RenewalState>(dto);
        renewalItem.SubscriptionId = GenerateId(dto);
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(dto.ProductId);
        renewalItem.ProductType = productInfo.ProductType;
        renewalItem.StartDate = DateTime.UtcNow;
        renewalItem.LastChargeDate = DateTime.UtcNow;
        switch (dto.RenewalPeriod)
        {
            case RenewalPeriod.OneMonth:
            {
                renewalItem.NextRenewalDate = GetNextMonthsFirstDay(1);
                renewalItem.PeriodicCost = productInfo.MonthlyUnitPrice * dto.ProductNumber * 1;
                break;
            }
            case RenewalPeriod.ThreeMonth:
            {
                renewalItem.NextRenewalDate = GetNextMonthsFirstDay(3);
                renewalItem.PeriodicCost = productInfo.MonthlyUnitPrice * dto.ProductNumber * 3;
                break;   
            }
            case RenewalPeriod.SixMonth:
            {
                renewalItem.NextRenewalDate = GetNextMonthsFirstDay(6);
                renewalItem.PeriodicCost = productInfo.MonthlyUnitPrice * dto.ProductNumber * 6;
                break;
            }
        }
        renewalItem.IsActive = true;
        //Check if a subscription of the same type already exists
        if (productInfo.ProductType == ProductType.ApiQueryCount)
        {
            var oldApiQueryRenewal = State.FirstOrDefault(r =>
                r.ProductId == dto.ProductId && r.OrganizationId == dto.OrganizationId &&
                r.IsActive == true);
            
            if (oldApiQueryRenewal == null)
            {
                
                State.Add(renewalItem);
                await WriteStateAsync();
                return _objectMapper.Map<RenewalState, RenewalDto>(renewalItem);
            }

            oldApiQueryRenewal.IsActive = false;
            State.Add(renewalItem);
            await WriteStateAsync();
            return _objectMapper.Map<RenewalState, RenewalDto>(renewalItem);
        }

        if (productInfo.ProductType == ProductType.FullPodResource)
        {
            var oldResourceRenewal = State.FirstOrDefault(r => r.AppId == dto.AppId &&
                                                               r.ProductType == productInfo.ProductType &&
                                                               r.OrganizationId == dto.OrganizationId &&
                                                               r.IsActive == true);
            if (oldResourceRenewal == null)
            {
                State.Add(renewalItem);
                await WriteStateAsync();
                return _objectMapper.Map<RenewalState, RenewalDto>(renewalItem);
            }
            
            oldResourceRenewal.IsActive = false;
            State.Add(renewalItem);
            await WriteStateAsync();
            return _objectMapper.Map<RenewalState, RenewalDto>(renewalItem);
        }

        return null;
    }

    private string GenerateId(CreateRenewalDto dto)
    {
        return Guid.NewGuid().ToString();
    }

    private DateTime GetNextMonthsFirstDay(int months)
    {
        DateTime today = DateTime.Today;
        DateTime firstOfNextMonths = new DateTime(today.Year, today.Month, 1).AddMonths(months);
        return firstOfNextMonths;
    }

    public async Task<RenewalDto> GetRenewalSubscriptionInfoByIdAsync(string subscriptionId)
    {
        var renewalState = State.FirstOrDefault(o => o.SubscriptionId == subscriptionId);
        var renewalDto = _objectMapper.Map<RenewalState, RenewalDto>(renewalState);
        return renewalDto;
    }

    public async Task UpdateRenewalDateToNextPeriodAsync(string subscriptionId)
    {
        await ReadStateAsync();
        var renewalState = State.FirstOrDefault(o => o.SubscriptionId == subscriptionId);
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalState.ProductId);
        switch (renewalState.RenewalPeriod)
        {
            case RenewalPeriod.OneMonth:
            {
                renewalState.NextRenewalDate = renewalState.NextRenewalDate.AddMonths(1);
                renewalState.PeriodicCost = productInfo.MonthlyUnitPrice * renewalState.ProductNumber * 1;
                break;
            }
            case RenewalPeriod.ThreeMonth:
            {
                renewalState.NextRenewalDate = renewalState.NextRenewalDate.AddMonths(3);
                renewalState.PeriodicCost = productInfo.MonthlyUnitPrice * renewalState.ProductNumber * 3;
                break;
            }
            case RenewalPeriod.SixMonth:
            {
                renewalState.NextRenewalDate = renewalState.NextRenewalDate.AddMonths(6);
                renewalState.PeriodicCost = productInfo.MonthlyUnitPrice * renewalState.ProductNumber * 6;
                break;
            }
        }

        renewalState.LastChargeDate = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task<RenewalDto> GetApiQueryCountRenewalInfoAsync(string organizationId, string userId,
        string productId)
    {
        var renewalState = State.FirstOrDefault(o =>
            o.OrganizationId == organizationId && o.UserId == userId && o.ProductId == productId && o.IsActive == true);
        var renewalDto = _objectMapper.Map<RenewalState, RenewalDto>(renewalState);
        return renewalDto;
    }

    public async Task<RenewalDto> GetPodResourceRenewalInfoAsync(string organizationId, string userId, string appId,
        string productId)
    {
        var renewalState = State.FirstOrDefault(o =>
            o.OrganizationId == organizationId && o.UserId == userId && o.AppId == appId && o.ProductId == productId &&
            o.IsActive == true);
        var renewalDto = _objectMapper.Map<RenewalState, RenewalDto>(renewalState);
        return renewalDto;
    }

    public async Task CancelRenewalByIdAsync(string subscriptionId)
    {
        await ReadStateAsync();
        var renewalState = State.FirstOrDefault(o => o.SubscriptionId == subscriptionId);
        renewalState.IsActive = false;
        await WriteStateAsync();
    }

    public async Task<int> GetUserMonthlyApiQueryAllowanceAsync(string organizationId,string userId)
    {
        var apiQueryCountSubscriptions =
            State.Where(r =>
                r.OrganizationId == organizationId && r.UserId == userId &&
                r.ProductType == ProductType.ApiQueryCount && r.IsActive == true).ToList();
        var totalQueryCount = 0;
        foreach (var renewalState in apiQueryCountSubscriptions)
        {
            var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
            var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalState.ProductId);
            var productQueryCount = Convert.ToInt32(productInfo.ProductSpecifications);
            totalQueryCount = totalQueryCount + (productQueryCount * renewalState.ProductNumber);
        }

        return totalQueryCount;
    }

    public async Task<string> GetCurrentSubscriptionIdAsync(string orderId)
    {
        var renewalState = State.FirstOrDefault(r => r.OrderId == orderId && r.IsActive == true);
        if (renewalState == null)
        {
            return null;
        }

        return renewalState.SubscriptionId;
    }
}