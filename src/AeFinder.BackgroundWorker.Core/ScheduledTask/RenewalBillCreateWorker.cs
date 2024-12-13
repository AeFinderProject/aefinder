using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class RenewalBillCreateWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IContractProvider _contractProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IAppDeployService _appDeployService;
    
    public RenewalBillCreateWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IContractProvider contractProvider,IOrganizationInformationProvider organizationInformationProvider,
        IUserInformationProvider userInformationProvider,IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,IAppDeployService appDeployService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _contractProvider = contractProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _userInformationProvider = userInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _appDeployService = appDeployService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = CalculateNextExecutionDelay();
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalAsync();
        
        Timer.Period = CalculateNextExecutionDelay();
    }

    private async Task ProcessRenewalAsync()
    {
        _logger.LogInformation("[RenewalBillCreateWorker] Process Renewal Bill Async.");
        var productsGrain = _clusterClient.GetGrain<IProductsGrain>(GrainIdHelper.GenerateProductsGrainId());
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[RenewalBillCreateWorker] Check organization: {0}.", organizationId);
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            var billsGrain =
                _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
            var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
            var renewalList = await renewalGrain.GetAllActiveRenewalInfosAsync(organizationId);
            foreach (var renewalDto in renewalList)
            {
                var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalDto.ProductId);
                //Skip free product
                if (productInfo.MonthlyUnitPrice == 0)
                {
                    continue;
                }
                
                //Charge for previous billing cycle
                var latestLockedBill = await billsGrain.GetLatestLockedBillAsync(renewalDto.OrderId);
                var chargeFee = latestLockedBill.BillingAmount;
                decimal refundAmount = 0;
                if (renewalDto.ProductType == ProductType.ApiQueryCount)
                {
                    //Charge based on usage query count
                    var monthlyQueryCount = 10;//TODO Get monthly query count
                    chargeFee = await billsGrain.CalculateApiQueryMonthlyChargeAmountAsync(monthlyQueryCount);
                    var monthlyFee = renewalDto.ProductNumber * productInfo.MonthlyUnitPrice;
                    refundAmount = monthlyFee - chargeFee;
                }
                
                //Send charge transaction to contract
                var userExtensionDto =
                    await _userInformationProvider.GetUserExtensionInfoByIdAsync(Guid.Parse(renewalDto.UserId));
                var organizationWalletAddress =
                    await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,userExtensionDto.WalletAddress);
                if (string.IsNullOrEmpty(organizationWalletAddress))
                {
                    _logger.LogError($"The organization wallet address has not yet been linked to user {renewalDto.UserId}");
                    throw new Exception("The organization wallet address has not yet been linked");
                }
                var chargeBill = await billsGrain.CreateChargeBillAsync(new CreateChargeBillDto()
                {
                    OrganizationId = organizationId,
                    OrderId = renewalDto.OrderId,
                    SubscriptionId = renewalDto.SubscriptionId,
                    ChargeFee = chargeFee,
                    Description = "Auto-renewal charge for the existing order.",
                    RefundAmount = refundAmount
                });
                await _contractProvider.BillingChargeAsync(organizationWalletAddress, chargeFee, 0,
                    chargeBill.BillingId);
                
                //Check user organization balance
                var userOrganizationBalanceInfoDto =
                    await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                        _contractOptions.BillingContractChainId);
                var organizationAccountBalance = userOrganizationBalanceInfoDto.UserBalance.Items[0].Balance;
                
                //If Balance not enough, freeze the AeIndexer, cancel the order & subscription
                if (organizationAccountBalance < renewalDto.PeriodicCost)
                {
                    var ordersGrain =
                        _clusterClient.GetGrain<IOrdersGrain>(organizationGrainId);
                    await ordersGrain.CancelOrderByIdAsync(renewalDto.OrderId);
                    if (renewalDto.ProductType == ProductType.FullPodResource)
                    {
                        await _appDeployService.FreezeAppAsync(renewalDto.AppId);
                    }
                    break;
                }
                
                //Lock for next billing cycle
                var lockFee = renewalDto.PeriodicCost;
                var newLockBill = await billsGrain.CreateSubscriptionLockBillAsync(new CreateSubscriptionBillDto()
                {
                    OrganizationId = organizationId,
                    SubscriptionId = renewalDto.SubscriptionId,
                    UserId = renewalDto.UserId,
                    AppId = renewalDto.AppId,
                    OrderId = renewalDto.OrderId,
                    Description = $"Auto-renewal lock for the existing order."
                });
                //Send lockFrom transaction to contract
                await _contractProvider.BillingLockFromAsync(organizationWalletAddress, newLockBill.BillingAmount,
                    newLockBill.BillingId);
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }

    private int CalculateNextExecutionDelay()
    {
        var now = DateTime.UtcNow;
        var firstDayNextMonth =
            new DateTime(now.Year, now.Month, _scheduledTaskOptions.RenewalBillDay, 2, 0, 0, DateTimeKind.Utc)
                .AddMonths(1);
        return (int)(firstDayNextMonth - now).TotalMilliseconds;
    }
}