using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market.Eto;
using AeFinder.User;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Market;

public class OrderService: ApplicationService, IOrderService
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppService _appService;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IBillService _billService;

    public OrderService(IClusterClient clusterClient, IOrganizationAppService organizationAppService,
        IContractProvider contractProvider, IDistributedEventBus distributedEventBus,
        IBillService billService,
        IAppOperationSnapshotProvider appOperationSnapshotProvider, IAppService appService)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _appService = appService;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _contractProvider = contractProvider;
        _distributedEventBus = distributedEventBus;
        _billService = billService;
    }

    public async Task<List<BillDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        var billList = new List<BillDto>();
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(dto.ProductId);
        dto.UserId = CurrentUser.Id.ToString();
        //TODO: Check organization id
        
        var organizationGrainId = await GetOrganizationGrainIdAsync(dto.OrganizationId);
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        
        //Calculate first month lock fee
        decimal firstMonthLockFee = 0;
        var billingPlan = await _billService.GetProductBillingPlanAsync(new GetBillingPlanInput()
        {
            OrganizationId = dto.OrganizationId,
            ProductId = dto.ProductId,
            ProductNum = dto.ProductNumber,
            PeriodMonths = dto.PeriodMonths
        });
        firstMonthLockFee = billingPlan.FirstMonthCost;
        decimal monthlyFee = dto.ProductNumber * productInfo.MonthlyUnitPrice;
        
        //Check if there is an existing order for a product of the same type
        OrderDto oldOrderInfo = null;
        if (productInfo.ProductType == ProductType.ApiQueryCount)
        {
            oldOrderInfo =
                await ordersGrain.GetLatestApiQueryCountOrderAsync(dto.OrganizationId);
        }

        if (productInfo.ProductType == ProductType.FullPodResource)
        {
            oldOrderInfo =
                await ordersGrain.GetLatestPodResourceOrderAsync(dto.OrganizationId, dto.AppId);
        }
        
        //If it exists, create a charge billing for the existing order
        if (oldOrderInfo != null)
        {
            if (oldOrderInfo.OrderStatus == OrderStatus.PendingPayment)
            {
                throw new UserFriendlyException(
                    "Please wait until the payment for the existing order is completed before initiating a new one.");
            }
            
            if (oldOrderInfo.OrderAmount == 0)
            {
                throw new UserFriendlyException(
                    "Can not reorder free product.");
            }
            
            //Calculate old order charge fee
            var subscriptionId = await renewalGrain.GetCurrentSubscriptionIdAsync(oldOrderInfo.OrderId);
            var renewalInfo = await renewalGrain.GetRenewalSubscriptionInfoByIdAsync(subscriptionId);
            var latestLockedBill = await billsGrain.GetLatestLockedBillAsync(oldOrderInfo.OrderId);
            var lockedAmount = latestLockedBill.BillingAmount;
            decimal chargeFee = 0;
            decimal refundAmount = 0;
            if (oldOrderInfo.ProductType == ProductType.FullPodResource)
            {
                //Charge based on usage duration
                DateTime? podResourceStartUseDay = null;
                podResourceStartUseDay = await _appOperationSnapshotProvider.GetAppPodStartTimeAsync(dto.AppId);
                chargeFee = await billsGrain.CalculatePodResourceMidWayChargeAmountAsync(renewalInfo, lockedAmount, podResourceStartUseDay);
            }

            if (oldOrderInfo.ProductType == ProductType.ApiQueryCount)
            {
                //Charge based on usage query count
                var monthlyQueryCount = 10;//TODO Get monthly query count
                chargeFee = await billsGrain.CalculateApiQueryMonthlyChargeAmountAsync(monthlyQueryCount);
            }
            var remainingLockedAmount = lockedAmount - chargeFee;
            if (firstMonthLockFee > remainingLockedAmount)
            {
                //Calculate new order need lock fee
                var needLockFee = firstMonthLockFee - remainingLockedAmount;
                //TODO Check user organization balance
                
                //Create new order
                var newOrder = await ordersGrain.CreateOrderAsync(dto);
                
                //Create lock bill
                var newLockBill = await billsGrain.CreateOrderLockBillAsync(new CreateOrderLockBillDto()
                {
                    OrganizationId = dto.OrganizationId,
                    UserId = dto.UserId,
                    AppId = dto.AppId,
                    OrderId = newOrder.OrderId,
                    LockFee = needLockFee,
                    Description =
                        $"Old order remaining locked amount: {remainingLockedAmount}, New order first month required locked amount: {firstMonthLockFee}, Additional amount needed to be locked: {needLockFee}."
                });
                await _distributedEventBus.PublishAsync(new BillCreateEto()
                {
                    OrganizationId = newLockBill.OrganizationId,
                    BillingId = newLockBill.BillingId
                });
                billList.Add(newLockBill);
            }
            else
            {
                //Calculate old order need refund fee
                refundAmount = remainingLockedAmount - firstMonthLockFee;
                // var refundBill = await billsGrain.CreateRefundBillAsync(new CreateRefundBillDto()
                // {
                //     OrganizationId = dto.OrganizationId,
                //     UserId = dto.UserId,
                //     AppId = dto.AppId,
                //     OrderId = oldOrderInfo.OrderId,
                //     SubscriptionId = subscriptionId,
                //     RefundFee = refundAmount,
                //     Description = "For the new order, refund the excess locked balance from the old order."
                // });
                // billList.Add(oldChargeBill);
            }

            var oldChargeBill = await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
            {
                OrganizationId = dto.OrganizationId,
                OrderId = oldOrderInfo.OrderId,
                SubscriptionId = subscriptionId,
                Description = "User creates a new order and processes billing settlement for the existing order.",
                ChargeFee = chargeFee,
                RefundAmount = refundAmount
            });
            await _distributedEventBus.PublishAsync(new BillCreateEto()
            {
                OrganizationId = oldChargeBill.OrganizationId,
                BillingId = oldChargeBill.BillingId
            });
        }
        else
        {
            if (monthlyFee == 0)
            {
                //Create new order
                var newFreeOrder = await ordersGrain.CreateOrderAsync(dto);
                await renewalGrain.CreateAsync(new CreateRenewalDto()
                {
                    OrganizationId = dto.OrganizationId,
                    UserId = dto.UserId,
                    AppId = dto.AppId,
                    OrderId = newFreeOrder.OrderId,
                    ProductId = dto.ProductId,
                    ProductNumber = 1,
                    RenewalPeriod = RenewalPeriod.OneMonth
                });
                return billList;
            }
            //TODO Check user organization balance
            
            //Create new order
            var newOrder = await ordersGrain.CreateOrderAsync(dto);
            var newLockBill = await billsGrain.CreateOrderLockBillAsync(new CreateOrderLockBillDto()
            {
                OrganizationId = dto.OrganizationId,
                UserId = dto.UserId,
                AppId = dto.AppId,
                OrderId = newOrder.OrderId,
                LockFee = firstMonthLockFee,
                Description = $"Lock a portion of the balance for the new order."
            });
            await _distributedEventBus.PublishAsync(new BillCreateEto()
            {
                OrganizationId = newLockBill.OrganizationId,
                BillingId = newLockBill.BillingId
            });
            billList.Add(newLockBill);
        }

        return billList;
    }
    
    // private async Task<string> GetOrganizationGrainIdAsync()
    // {
    //     var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
    //     return organizationIds.First().Id.ToString("N");
    // }

    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }
}