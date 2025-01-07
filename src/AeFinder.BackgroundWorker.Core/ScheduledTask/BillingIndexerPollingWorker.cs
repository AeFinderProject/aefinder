using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Commons;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Merchandises;
using AeFinder.Options;
using AeFinder.Orders;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class BillingIndexerPollingWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<BillingIndexerPollingWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;
    private readonly IOrderService _orderService;
    private readonly IBillingService _billingService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAssetService _assetService;
    private readonly IAppDeployService _appDeployService;
    private readonly IBillingContractProvider _billingContractProvider;
    private readonly IUserAppService _userAppService;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IBillingEmailSender _billingEmailSender;
    private readonly IAppEmailSender _appEmailSender;
    
    public BillingIndexerPollingWorker(AbpAsyncTimer timer, 
        ILogger<BillingIndexerPollingWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IOrganizationInformationProvider organizationInformationProvider,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOrderService orderService,
        IBillingService billingService,
        IAssetService assetService,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService,
        IBillingContractProvider billingContractProvider,
        IUserAppService userAppService,
        IUserInformationProvider userInformationProvider,
        IBillingEmailSender billingEmailSender,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _organizationInformationProvider = organizationInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
        _orderService = orderService;
        _billingService = billingService;
        _assetService = assetService;
        _graphQlOptions = graphQlOptions.Value;
        _appDeployService = appDeployService;
        _billingContractProvider = billingContractProvider;
        _userAppService = userAppService;
        _userInformationProvider = userInformationProvider;
        _billingEmailSender = billingEmailSender;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.BillingIndexerPollingTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessIndexerPollingAsync();
    }

    private async Task ProcessIndexerPollingAsync()
    {
        _logger.LogInformation("[BillingIndexerPollingWorker] Process indexer polling Async.");
        
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            var now = DateTime.UtcNow;
            
            //Check organization wallet address is bind
            var organizationWalletAddress =
                await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
            if (string.IsNullOrEmpty(organizationWalletAddress))
            {
                // _logger.LogWarning($"Organization {organizationId} wallet address is null or empty, please check.");
                continue;
            }
            
            // //Handle payment orders
            // var paymentOrders = await GetPaymentOrderListAsync(organizationUnitDto.Id);
            // foreach (var paymentOrder in paymentOrders)
            // {
            //     await HandlePaymentOrderAsync(organizationUnitDto.Id, paymentOrder);
            // }

            // //Handle advance payment bills
            // var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);
            // var nextMonth = now.AddMonths(1);
            // var firstDayOfNextMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
            // var lastDayOfThisMonth = firstDayOfNextMonth.AddDays(-1);
            // var advanceBillBeginTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
            // var advanceBillEndTime = new DateTime(lastDayOfThisMonth.Year, lastDayOfThisMonth.Month, lastDayOfThisMonth.Day,
            //     23, 59, 59);
            // var advancePaymentBills =
            //     await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.AdvancePayment,
            //         advanceBillBeginTime, advanceBillEndTime);
            // foreach (var advancePaymentBill in advancePaymentBills)
            // {
            //     await HandleAdvancePaymentBillAsync(advancePaymentBill);
            // }
            
            // //Handle settlement bills
            // var previousMonth = now.AddMonths(-1);
            // var lastDayOfLastMonth = firstDayOfThisMonth.AddDays(-1);
            // var billBeginTime = new DateTime(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0);
            // var billEndTime = new DateTime(lastDayOfLastMonth.Year, lastDayOfLastMonth.Month, lastDayOfLastMonth.Day,
            //     23, 59, 59);
            // var settlementBills = await GetPaymentBillingListAsync(organizationUnitDto.Id, BillingType.Settlement,
            //     billBeginTime, billEndTime);
            // foreach (var settlementBill in settlementBills)
            // {
            //     await HandleSettlementBillAsync(organizationUnitDto.Id, organizationName, settlementBill);
            // }

        }
        
    }

    

    

    

    


}