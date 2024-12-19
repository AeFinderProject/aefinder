using AeFinder.BackgroundWorker.Options;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class RenewalBalanceCheckWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    
    public RenewalBalanceCheckWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.AppInfoSyncTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalBalanceCheckAsync();
    }

    private async Task ProcessRenewalBalanceCheckAsync()
    {
        _logger.LogInformation("[RenewalBalanceCheckWorker] Process Renewal Balance Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            
        }
    }
}