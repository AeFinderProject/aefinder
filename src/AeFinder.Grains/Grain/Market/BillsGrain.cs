using AeFinder.Grains.State.Market;
using AeFinder.Market;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Market;

public class BillsGrain: AeFinderGrain<List<BillState>>, IBillsGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public BillsGrain(IObjectMapper objectMapper)
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

    public async Task<BillDto> CreateOrderLockBillAsync(CreateOrderLockBillDto dto)
    {
        await ReadStateAsync();
        if (State == null)
        {
            State = new List<BillState>();
        }
        
        var billItem=_objectMapper.Map<CreateOrderLockBillDto, BillState>(dto);
        billItem.BillingId = GenerateId();
        billItem.BillingType = BillingType.Lock;
        billItem.BillingDate = DateTime.UtcNow;
        if (dto.LockFee > 0)
        {
            billItem.BillingAmount = dto.LockFee;
        }
        else
        {
            var ordersGrain =
                GrainFactory.GetGrain<IOrdersGrain>(GrainIdHelper.GenerateOrdersGrainId(Guid.Parse(dto.OrganizationId)));
            var orderInfo = await ordersGrain.GetOrderByIdAsync(dto.OrderId);
            var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
            var productInfo = await productsGrain.GetProductInfoByIdAsync(orderInfo.ProductId);
            decimal monthlyFee = orderInfo.ProductNumber * productInfo.MonthlyUnitPrice;
            billItem.BillingAmount = await CalculateFirstMonthAmount(monthlyFee);
        }
        
        billItem.BillingStatus = BillingStatus.PendingPayment;
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }

    public async Task<BillDto> CreateSubscriptionLockBillAsync(CreateSubscriptionBillDto dto)
    {
        await ReadStateAsync();
        if (State == null)
        {
            State = new List<BillState>();
        }
        var billItem=_objectMapper.Map<CreateSubscriptionBillDto, BillState>(dto);
        billItem.BillingId = GenerateId();
        billItem.BillingType = BillingType.Lock;
        billItem.BillingDate = DateTime.UtcNow;
        var renewalGrain =
            GrainFactory.GetGrain<IRenewalGrain>(GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(dto.OrganizationId)));
        var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(dto.SubscriptionId);
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        switch (renewalInfo.RenewalPeriod)
        {
            case RenewalPeriod.OneMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 1;
                break;
            }
            case RenewalPeriod.ThreeMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 3;
                break;
            }
            case RenewalPeriod.SixMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 6;
                break;
            }
        }

        billItem.BillingStatus = BillingStatus.PendingPayment;
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }

    public async Task<BillDto> CreateChargeBillAsync(string organizationId, string subscriptionId, string description,
        decimal chargeFee)
    {
        var billItem = new BillState();
        billItem.BillingId = GenerateId();
        billItem.OrganizationId = organizationId;
        billItem.SubscriptionId = subscriptionId;
        var renewalGrain =
            GrainFactory.GetGrain<IRenewalGrain>(GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(subscriptionId);
        billItem.UserId = renewalInfo.UserId;
        billItem.AppId = renewalInfo.AppId;
        billItem.BillingType = BillingType.Charge;
        billItem.BillingDate = DateTime.UtcNow;
        billItem.Description = description;
        if (chargeFee > 0)
        {
            billItem.BillingAmount = chargeFee;
        }
        else
        {
            var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
            var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
            decimal monthlyFee = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice;
            billItem.BillingAmount = await CalculateChargeAmount(renewalInfo, monthlyFee);
        }
        billItem.BillingStatus = BillingStatus.PendingPayment;
        await ReadStateAsync();
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }

    public async Task<BillDto> GetLatestLockedBillAsync(string subscriptionId)
    {
        var latestLockedBill= State.Where(b => b.SubscriptionId == subscriptionId && b.BillingType == BillingType.Lock)
            .OrderByDescending(o => o.BillingDate).FirstOrDefault();
        if (latestLockedBill == null)
        {
            return null;
        }
        return _objectMapper.Map<BillState, BillDto>(latestLockedBill);
    }

    public async Task<BillDto> CreateRefundBillAsync(CreateRefundBillDto dto)
    {
        await ReadStateAsync();
        var billItem=_objectMapper.Map<CreateRefundBillDto, BillState>(dto);
        billItem.BillingId = GenerateId();
        billItem.BillingType = BillingType.Refund;
        billItem.BillingDate = DateTime.UtcNow;
        billItem.BillingAmount = dto.RefundFee;
        billItem.BillingStatus = BillingStatus.PendingPayment;
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }
    
    private string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }
    
    //The first payment only requires covering the usage fees for the current month.
    public async Task<decimal> CalculateFirstMonthAmount(decimal monthlyFee)
    {
        DateTime today = DateTime.Today;
        DateTime firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        int daysUntilNextMonth = (firstOfNextMonth - today).Days;
        int daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
        decimal dailyFee = monthlyFee / daysInCurrentMonth;
        decimal amountDue = dailyFee * daysUntilNextMonth;
        return amountDue;
    }

    public async Task<decimal> CalculateChargeAmount(RenewalDto renewalInfo, decimal monthlyFee)
    {
        var lastBillingDate = renewalInfo.LastChargeDate;
        var endDate = DateTime.UtcNow;
        int daysUsed = (endDate - lastBillingDate).Days + 1;
        int daysInLastBillingMonth = DateTime.DaysInMonth(lastBillingDate.Year, lastBillingDate.Month);
        decimal dailyFee = monthlyFee / daysInLastBillingMonth;
        decimal usedFee = dailyFee * daysUsed;
        return usedFee;
    }

    public async Task<BillDto> GetBillByIdAsync(string billingId)
    {
        var billState = State.FirstOrDefault(o => o.BillingId == billingId);
        var billDto = _objectMapper.Map<BillState, BillDto>(billState);
        return billDto;
    }
}