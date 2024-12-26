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

public class MonthlyAutomaticChargeWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<MonthlyAutomaticChargeWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    
    public MonthlyAutomaticChargeWorker(AbpAsyncTimer timer, 
        ILogger<MonthlyAutomaticChargeWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.MonthlyAutomaticChargeTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var now = DateTime.UtcNow;
        if (now.Day == _scheduledTaskOptions.MonthlyAutomaticChargeDay)
        {
            _logger.LogInformation($"[MonthlyAutomaticChargeWorker] Today is the {_scheduledTaskOptions.MonthlyAutomaticChargeDay}th day of the month. Executing task.");
            await ProcessChargeCheckAsync();
        }
        else
        {
            _logger.LogInformation($"[MonthlyAutomaticChargeWorker] Today is not the {_scheduledTaskOptions.MonthlyAutomaticChargeDay}th day of the month. Task will not be executed.");
        }
    }

    private async Task ProcessChargeCheckAsync()
    {
        _logger.LogInformation("[MonthlyAutomaticChargeWorker] Process Charge Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[MonthlyAutomaticChargeWorker] Check organization {0} {1}.", organizationName,
                organizationId);
            
        }
    }
}