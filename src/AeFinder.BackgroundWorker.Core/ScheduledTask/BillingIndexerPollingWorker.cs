using AeFinder.BackgroundWorker.Core.Provider;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class BillingIndexerPollingWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<BillingIndexerPollingWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillTransactionPollingProvider _billTransactionPollingProvider;

    public BillingIndexerPollingWorker(AbpAsyncTimer timer, ILogger<BillingIndexerPollingWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IClusterClient clusterClient, IOrganizationAppService organizationAppService,
        IBillTransactionPollingProvider billTransactionPollingProvider,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _billTransactionPollingProvider = billTransactionPollingProvider;
        // Timer.Period = 10 * 1000; // 10000 milliseconds = 10 seconds
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
            
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            var billsGrain =
                _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
            var pendingBills = await billsGrain.GetAllPendingBillAsync();
            if (pendingBills == null || pendingBills.Count == 0)
            {
                continue;
            }
            
            foreach (var billDto in pendingBills)
            {
                _logger.LogInformation(
                    "[RenewalBillCreateWorker] Organization: {0} {1} has pending {2} bill {3} of order: {4}",
                    organizationName, billDto.OrganizationId, billDto.BillingType.ToString(), billDto.BillingId,
                    billDto.OrderId);
                await _billTransactionPollingProvider.HandleTransactionAsync(billDto.BillingId, billDto.OrganizationId);
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }
}