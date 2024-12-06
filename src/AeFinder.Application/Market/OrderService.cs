using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.User;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AeFinder.Market;

public class OrderService: ApplicationService, IOrderService
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppService _appService;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;

    public OrderService(IClusterClient clusterClient, IOrganizationAppService organizationAppService,
        IAppOperationSnapshotProvider appOperationSnapshotProvider,IAppService appService)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _appService = appService;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
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
        OrderDto oldOrderInfo = null;
        DateTime? podResourceStartUseDay = null;
        if (productInfo.ProductType == ProductType.ApiQueryCount)
        {
            //Check if there is an existing order for a product of the same type
            oldOrderInfo =
                await ordersGrain.GetLatestApiQueryCountOrderAsync(dto.OrganizationId, dto.UserId);
        }

        if (productInfo.ProductType == ProductType.FullPodResource)
        {
            //Check if there is an existing order for a product of the same type
            oldOrderInfo =
                await ordersGrain.GetLatestPodResourceOrderAsync(dto.OrganizationId, dto.UserId, dto.AppId);
            podResourceStartUseDay = await _appOperationSnapshotProvider.GetAppPodStartTimeAsync(dto.AppId);
        }
        
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
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
            var chargeFee = await billsGrain.CalculateMidWayChargeAmount(renewalInfo, lockedAmount, podResourceStartUseDay);
            decimal refundAmount = 0;
            
            //Create new order
            var newOrder = await ordersGrain.CreateOrderAsync(dto);
            var remainingLockedAmount = lockedAmount - chargeFee;
            decimal monthlyFee = dto.ProductNumber * productInfo.MonthlyUnitPrice;
            var firstMonthFee = await billsGrain.CalculateFirstMonthLockAmount(monthlyFee);
            if (firstMonthFee > remainingLockedAmount)
            {
                //Calculate new order need lock fee
                var needLockFee = firstMonthFee - remainingLockedAmount;
                var newLockBill = await billsGrain.CreateOrderLockBillAsync(new CreateOrderLockBillDto()
                {
                    OrganizationId = dto.OrganizationId,
                    UserId = dto.UserId,
                    AppId = dto.AppId,
                    OrderId = newOrder.OrderId,
                    LockFee = needLockFee,
                    Description = $"Old order remaining locked amount: {remainingLockedAmount}, New order first month required locked amount: {firstMonthFee}, Additional amount needed to be locked: {needLockFee}."
                });
                billList.Add(newLockBill);
            }
            else
            {
                //Calculate old order need refund fee
                refundAmount = remainingLockedAmount - firstMonthFee;
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
            
            var oldChargeBill = await billsGrain.CreateChargeBillAsync(dto.OrganizationId, subscriptionId,
                "User creates a new order and processes billing settlement for the existing order.", chargeFee,
                refundAmount);
            //TODO: send charge transaction to contract
            
        }
        else
        {
            //Create new order
            var newOrder = await ordersGrain.CreateOrderAsync(dto);
            decimal monthlyFee = dto.ProductNumber * productInfo.MonthlyUnitPrice;
            if (monthlyFee == 0)
            {
                await renewalGrain.CreateAsync(new CreateRenewalDto()
                {
                    OrganizationId = dto.OrganizationId,
                    UserId = dto.UserId,
                    AppId = dto.AppId,
                    OrderId = newOrder.OrderId,
                    ProductId = dto.ProductId,
                    ProductNumber = 1,
                    RenewalPeriod = RenewalPeriod.OneMonth
                });
                return billList;
            }
            var firstMonthFee = await billsGrain.CalculateFirstMonthLockAmount(monthlyFee);
            var newLockBill = await billsGrain.CreateOrderLockBillAsync(new CreateOrderLockBillDto()
            {
                OrganizationId = dto.OrganizationId,
                UserId = dto.UserId,
                AppId = dto.AppId,
                OrderId = newOrder.OrderId,
                LockFee = firstMonthFee,
                Description = $"Lock a portion of the balance for the new order."
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