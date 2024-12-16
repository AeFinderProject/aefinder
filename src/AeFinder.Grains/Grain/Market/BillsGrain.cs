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
            billItem.BillingAmount = await CalculateFirstMonthLockAmount(monthlyFee);
        }

        billItem.BillingStartDate = DateTime.UtcNow;
        billItem.BillingEndDate = GetFirstDayOfNextMonths(billItem.BillingStartDate, 1);
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
        billItem.BillingType = BillingType.LockFrom;
        billItem.BillingDate = DateTime.UtcNow;
        var renewalGrain =
            GrainFactory.GetGrain<IRenewalGrain>(GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(dto.OrganizationId)));
        var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(dto.SubscriptionId);
        var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        billItem.BillingStartDate = renewalInfo.NextRenewalDate;
        switch (renewalInfo.RenewalPeriod)
        {
            case RenewalPeriod.OneMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 1;
                billItem.BillingEndDate = GetFirstDayOfNextMonths(billItem.BillingStartDate, 1);
                break;
            }
            case RenewalPeriod.ThreeMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 3;
                billItem.BillingEndDate = GetFirstDayOfNextMonths(billItem.BillingStartDate, 3);
                break;
            }
            case RenewalPeriod.SixMonth:
            {
                billItem.BillingAmount = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice * 6;
                billItem.BillingEndDate = GetFirstDayOfNextMonths(billItem.BillingStartDate, 6);
                break;
            }
        }
        billItem.BillingStatus = BillingStatus.PendingPayment;
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }

    public async Task<BillDto> CreateChargeBillAsync(CreateChargeBillDto dto)
    {
        var billItem=_objectMapper.Map<CreateChargeBillDto, BillState>(dto);
        billItem.BillingId = GenerateId();
        var renewalGrain =
            GrainFactory.GetGrain<IRenewalGrain>(GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(dto.OrganizationId)));
        var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(dto.SubscriptionId);
        billItem.UserId = renewalInfo.UserId;
        billItem.AppId = renewalInfo.AppId;
        billItem.BillingType = BillingType.Charge;
        billItem.BillingDate = DateTime.UtcNow;
        // if (chargeFee > 0)
        // {
        billItem.BillingAmount = dto.ChargeFee;
        // }
        // else
        // {
        //     var productsGrain = GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        //     var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        //     decimal monthlyFee = renewalInfo.ProductNumber * productInfo.MonthlyUnitPrice;
        //     billItem.BillingAmount = await CalculateChargeAmount(renewalInfo, monthlyFee);
        // }

        billItem.BillingStartDate = renewalInfo.LastChargeDate;
        billItem.BillingEndDate = DateTime.UtcNow;
        billItem.RefundAmount = dto.RefundAmount;
        billItem.BillingStatus = BillingStatus.PendingPayment;
        await ReadStateAsync();
        State.Add(billItem);
        await WriteStateAsync();
        return _objectMapper.Map<BillState, BillDto>(billItem);
    }

    public async Task<BillDto> GetLatestLockedBillAsync(string orderId)
    {
        var latestLockedBill = State.Where(b =>
                b.OrderId == orderId && b.BillingType == BillingType.Lock && b.BillingStatus == BillingStatus.Paid)
            .OrderByDescending(o => o.BillingDate).FirstOrDefault();
        if (latestLockedBill == null)
        {
            return null;
        }
        return _objectMapper.Map<BillState, BillDto>(latestLockedBill);
    }
    
    private string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }
    
    //The first payment only requires covering the usage fees for the current month.
    public async Task<decimal> CalculateFirstMonthLockAmount(decimal monthlyFee)
    {
        DateTime today = DateTime.UtcNow;
        DateTime firstOfNextMonth = GetFirstDayOfNextMonths(today, 1);
        int daysUntilNextMonth = (firstOfNextMonth - today).Days;
        int daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
        decimal dailyFee = monthlyFee / daysInCurrentMonth;
        decimal amountDue = dailyFee * daysUntilNextMonth;
        return amountDue;
    }

    private DateTime GetFirstDayOfNextMonths(DateTime today, int months)
    {
        DateTime firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(months);
        return firstOfNextMonth;
    }

    public async Task<decimal> CalculatePodResourceMidWayChargeAmountAsync(RenewalDto renewalInfo, decimal lockedAmount,
        DateTime? podResourceStartUseDay)
    {
        if (renewalInfo.ProductType == ProductType.FullPodResource && podResourceStartUseDay == null)
        {
            return 0;
        }

        var lastBillingDate = renewalInfo.LastChargeDate;
        var endDate = DateTime.UtcNow;
        int daysUsed = (endDate - lastBillingDate).Days + 1;
        if (podResourceStartUseDay != null && podResourceStartUseDay.Value > lastBillingDate)
        {
            //Pod resource usage duration should be calculated starting from the time the first Pod subscription begins.
            daysUsed = (endDate - podResourceStartUseDay.Value).Days + 1;
        }

        int daysInLastBillingMonth = DateTime.DaysInMonth(lastBillingDate.Year, lastBillingDate.Month);
        decimal dailyFee = lockedAmount / daysInLastBillingMonth;
        decimal usedFee = dailyFee * daysUsed;
        return usedFee;
    }

    public async Task<decimal> CalculateApiQueryMonthlyChargeAmountAsync(long monthlyQueryCount)
    {
        var productsGrain =
            GrainFactory.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var regularProductInfo = await productsGrain.GetRegularApiQueryCountProductAsync();
        var singleQueryUnitPrice = regularProductInfo.MonthlyUnitPrice /
                                   Convert.ToInt32(regularProductInfo.ProductSpecifications);
        var usedFee = singleQueryUnitPrice * monthlyQueryCount;
        return usedFee;
    }

    public async Task<BillDto> GetBillByIdAsync(string billingId)
    {
        var billState = State.FirstOrDefault(o => o.BillingId == billingId);
        var billDto = _objectMapper.Map<BillState, BillDto>(billState);
        return billDto;
    }

    public async Task<BillDto> UpdateBillingTransactionInfoAsync(string billingId, string transactionId,
        decimal transactionAmount, string walletAddress)
    {
        var billState = State.FirstOrDefault(o => o.BillingId == billingId);
        billState.TransactionId = transactionId;
        billState.TransactionAmount = transactionAmount;
        billState.WalletAddress = walletAddress;
        if (transactionAmount == billState.BillingAmount)
        {
            billState.BillingStatus = BillingStatus.Paid;
        }
        else
        {
            billState.BillingStatus = BillingStatus.PartiallyPaid;
        }

        var billDto = _objectMapper.Map<BillState, BillDto>(billState);
        return billDto;
    }

    public async Task<BillDto> GetPendingChargeBillByOrderIdAsync(string orderId)
    {
        var billState = State.FirstOrDefault(b =>
            b.OrderId == orderId && b.BillingType == BillingType.Charge &&
            b.BillingStatus == BillingStatus.PendingPayment);
        if (billState == null)
        {
            return null;
        }
        var billDto = _objectMapper.Map<BillState, BillDto>(billState);
        return billDto;
    }

    public async Task<List<BillDto>> GetOrganizationAllBillsAsync(string organizationId)
    {
        var bills = State.Where(b => b.OrganizationId == organizationId).ToList();
        var billDtoList = _objectMapper.Map<List<BillState>, List<BillDto>>(bills);
        return billDtoList;
    }

    public async Task<List<BillDto>> GetAllPendingBillAsync()
    {
        var bills = State.Where(b => b.BillingStatus == BillingStatus.PendingPayment).OrderBy(b => b.BillingDate)
            .ToList();
        var billDtoList = _objectMapper.Map<List<BillState>, List<BillDto>>(bills);
        return billDtoList;
    }
}