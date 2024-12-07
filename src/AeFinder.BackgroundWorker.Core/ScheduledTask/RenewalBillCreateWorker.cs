using AeFinder.BackgroundWorker.Options;
using AeFinder.Common;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.User;
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
    
    public RenewalBillCreateWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IContractProvider contractProvider,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _contractProvider = contractProvider;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.AppInfoSyncTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalAsync();
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
                var chargeBill = await billsGrain.CreateChargeBillAsync(organizationId, renewalDto.SubscriptionId,
                    "Auto-renewal charge for the existing order.", chargeFee,
                    0);
                //TODO: send charge transaction to contract
                
                
                //TODO: Check user balance
                
                
                //TODO: If Balance not enough, freeze the AeIndexer, cancel the order & subscription
                
                
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
                //TODO: send lock transaction to contract
                
                
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }
}