using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Apps;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Amazon.Runtime.Internal.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IRenewalService _renewalService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;

    public OrderService(IClusterClient clusterClient, IOrganizationAppService organizationAppService,
        IContractProvider contractProvider, IDistributedEventBus distributedEventBus,
        IBillService billService, IApiKeyService apiKeyService,
        IUserInformationProvider userInformationProvider,IRenewalService renewalService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider, IOptionsSnapshot<ContractOptions> contractOptions,
        IAppOperationSnapshotProvider appOperationSnapshotProvider, IAppService appService)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _appService = appService;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _contractProvider = contractProvider;
        _distributedEventBus = distributedEventBus;
        _billService = billService;
        _apiKeyService = apiKeyService;
        _userInformationProvider = userInformationProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _renewalService = renewalService;
    }

    public async Task<List<BillDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        var billList = new List<BillDto>();
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(dto.ProductId);
        if (productInfo == null)
        {
            throw new UserFriendlyException($"Invalid product id {dto.ProductId}");
        }

        if (productInfo.MonthlyUnitPrice == 0)
        {
            throw new UserFriendlyException(
                "Can not reorder free product.");
        }

        dto.UserId = CurrentUser.Id.ToString();
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        dto.OrganizationId = organizationUnit.Id.ToString();

        var organizationGrainId = GrainIdHelper.GetOrganizationGrainIdAsync(dto.OrganizationId);
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);

        //Calculate first month lock fee
        decimal firstMonthLockFee = 0;
        var billingPlan = await _billService.GetProductBillingPlanAsync(new GetBillingPlanInput()
        {
            ProductId = dto.ProductId,
            ProductNum = dto.ProductNumber,
            PeriodMonths = dto.PeriodMonths
        });
        firstMonthLockFee = billingPlan.FirstMonthCost;
        decimal monthlyFee = dto.ProductNumber * productInfo.MonthlyUnitPrice;

        //Get user organization account balance
        var userExtensionDto =
            await _userInformationProvider.GetUserExtensionInfoByIdAsync(CurrentUser.Id.Value);
        if (userExtensionDto.WalletAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Please bind your user wallet first.");
        }

        var organizationWalletAddress =
            await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(dto.OrganizationId,
                userExtensionDto.WalletAddress);
        if (string.IsNullOrEmpty(organizationWalletAddress))
        {
            throw new UserFriendlyException(
                $"The user has not linked any organization wallet address yet. Please deposit your account first.");
        }

        Logger.LogInformation(
            $"userWalletAddress:{userExtensionDto.WalletAddress} organizationWalletAddress:{organizationWalletAddress}");
        var userOrganizationBalanceInfoDto =
            await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                _contractOptions.BillingContractChainId, 0, 10);
        var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
        // var lockedBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].LockedBalance;
        // var unLockedBalance = organizationAccountBalance - lockedBalance;

        //Check if the purchase quantity is less than the quantity already used.
        if (productInfo.ProductType == ProductType.ApiQueryCount)
        {
            var freeApiQueryCount = await _renewalService.GetUserApiQueryFreeCountAsync();
            var orderApiQueryCount = (dto.ProductNumber * Convert.ToInt32(productInfo.ProductSpecifications)) +
                                     freeApiQueryCount;
            var organizationGuid = Guid.Parse(dto.OrganizationId);
            var monthlyQueryCount = await _apiKeyService.GetMonthQueryCountAsync(organizationGuid, DateTime.UtcNow);
            if (monthlyQueryCount > orderApiQueryCount)
            {
                throw new UserFriendlyException(
                    "Order failed: The API query count already used by the user exceeds the purchased query count. Please adjust accordingly.");
            }
        }

        //Check user organization balance
        if (organizationAccountBalance < firstMonthLockFee)
        {
            throw new UserFriendlyException("The user's organization account has insufficient balance.");
        }

        //Create new order
        var newOrder = await ordersGrain.CreateOrderAsync(dto);
        var newLockBill = await billsGrain.CreateOrderLockBillAsync(new CreateOrderLockBillDto()
        {
            OrganizationId = dto.OrganizationId,
            UserId = dto.UserId,
            AppId = dto.AppId,
            OrderId = newOrder.OrderId,
            LockFee = firstMonthLockFee,
            Description = $"Lock a portion of the balance for the new [{productInfo.ProductType.ToString()}] order."
        });
        billList.Add(newLockBill);

        return billList;
    }

    public async Task UpdateBillToPendingStatusAsync(string billingId)
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainIdAsync(organizationId);
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        
        //Get bill & order info
        var billDto = await billsGrain.GetBillByIdAsync(billingId);
        if (billDto == null)
        {
            throw new UserFriendlyException("invalid billing id");
        }
        var orderDto = await ordersGrain.GetOrderByIdAsync(billDto.OrderId);

        //Update bill to pending status
        await billsGrain.UpdateBillToPendingStatusAsync(billingId);
        //Update order to pending status
        await ordersGrain.UpdateOrderToPendingStatusAsync(billDto.OrderId);

        //Create charge bill
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(orderDto.ProductId);
        if (productInfo == null)
        {
            throw new UserFriendlyException($"Invalid product id {orderDto.ProductId}");
        }
        //Check if there is an existing order for a product of the same type
        OrderDto oldOrderInfo = null;
        if (orderDto.ProductType == ProductType.ApiQueryCount)
        {
            oldOrderInfo =
                await ordersGrain.GetLatestApiQueryCountOrderAsync(organizationId);
        }

        if (orderDto.ProductType == ProductType.FullPodResource)
        {
            oldOrderInfo =
                await ordersGrain.GetLatestPodResourceOrderAsync(organizationId, orderDto.AppId);
        }
        //If it exists, create a charge billing for the existing order
        if (oldOrderInfo != null)
        {
            if (oldOrderInfo.OrderStatus == OrderStatus.PendingPayment)
            {
                throw new UserFriendlyException(
                    "Please wait until the payment for the existing order is completed before initiating a new one.");
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
                podResourceStartUseDay = await _appOperationSnapshotProvider.GetAppPodStartTimeAsync(orderDto.AppId);
                chargeFee = await billsGrain.CalculatePodResourceMidWayChargeAmountAsync(renewalInfo, lockedAmount, podResourceStartUseDay);
                refundAmount = lockedAmount - chargeFee;
            }
            
            if (oldOrderInfo.ProductType == ProductType.ApiQueryCount)
            {
                var oldOrderMonthlyFee = oldOrderInfo.ProductNumber * productInfo.MonthlyUnitPrice;
                //The API query count product is settled monthly in the background scheduled tasks.
                chargeFee = 0;
                refundAmount =
                    (12 * (renewalInfo.NextRenewalDate.Year - DateTime.UtcNow.Year) +
                        renewalInfo.NextRenewalDate.Month - DateTime.UtcNow.Month) * oldOrderMonthlyFee;
                // //Charge based on usage query count
                // var organizationGuid = Guid.Parse(dto.OrganizationId);
                // var monthlyQueryCount = await _apiKeyService.GetMonthQueryCountAsync(organizationGuid, DateTime.UtcNow);
                // Logger.LogInformation($"[CreateOrderAsync]Api monthly query count:{monthlyQueryCount} time:{DateTime.UtcNow.ToString()}");
                // chargeFee = await billsGrain.CalculateApiQueryMonthlyChargeAmountAsync(monthlyQueryCount);
            }
            
            await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
            {
                OrganizationId = organizationId,
                OrderId = oldOrderInfo.OrderId,
                SubscriptionId = subscriptionId,
                Description = $"Creates a [{productInfo.ProductType.ToString()}] charge bill for the existing order.",
                ChargeFee = chargeFee,
                RefundAmount = refundAmount
            });
        }
    }

    public async Task CancelCreatedBillAsync(string billingId)
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainIdAsync(organizationId);
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        
        //Get bill & order info
        var billDto = await billsGrain.GetBillByIdAsync(billingId);
        if (billDto == null)
        {
            throw new UserFriendlyException("invalid billing id");
        }
        var orderDto = await ordersGrain.GetOrderByIdAsync(billDto.OrderId);
        
        //Cancel bill
        await billsGrain.CancelCreatedBillAsync(billingId);

        //Cancel order
        await ordersGrain.CancelCreatedOrderByIdAsync(orderDto.OrderId);
    }

    public async Task CancelOrderAndBillAsync(string organizationId, string orderId, string billingId)
    {
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainIdAsync(organizationId);
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        await ordersGrain.CancelOrderByIdAsync(orderId);
        Logger.LogInformation($"Order {orderId} canceled");
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        await renewalGrain.CancelRenewalByOrderIdAsync(orderId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        await billsGrain.CancelBillAsync(billingId);
        Logger.LogInformation($"Bill {billingId} canceled");
    }

    // private async Task<string> GetOrganizationGrainIdAsync()
    // {
    //     var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
    //     return organizationIds.First().Id.ToString("N");
    // }
    

    public async Task OrderFreeApiQueryCountAsync(string organizationId)
    {
        var organizationGrainId = GrainIdHelper.GetOrganizationGrainIdAsync(organizationId);
        //Automatically place an order for a free API query package for the organization.
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var freeProduct = await productsGrain.GetFreeApiQueryCountProductAsync();
        var ordersGrain =
            _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
        var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
        //Check 
        if (await renewalGrain.CheckRenewalInfoIsExistAsync(organizationId, freeProduct.ProductId))
        {
            throw new UserFriendlyException("The user's organization is already equipped with a free API query allowance.");
        }
        
        var newFreeOrder = await ordersGrain.CreateOrderAsync(new CreateOrderDto()
        {
            OrganizationId = organizationId,
            AppId = String.Empty,
            ProductId = freeProduct.ProductId,
            UserId = CurrentUser.Id.ToString(),
            ProductNumber = 1,
            PeriodMonths = 1
        });
        
        await renewalGrain.CreateAsync(new CreateRenewalDto()
        {
            OrganizationId = organizationId,
            UserId = CurrentUser.Id.ToString(),
            AppId = String.Empty,
            OrderId = newFreeOrder.OrderId,
            ProductId = freeProduct.ProductId,
            ProductNumber = 1,
            RenewalPeriod = RenewalPeriod.OneMonth
        });
        var queryAllowance = await _renewalService.GetUserMonthlyApiQueryAllowanceAsync(organizationId);
        var organizationGuid = Guid.Parse(organizationId);
        await _apiKeyService.SetQueryLimitAsync(organizationGuid, queryAllowance);
    }
}