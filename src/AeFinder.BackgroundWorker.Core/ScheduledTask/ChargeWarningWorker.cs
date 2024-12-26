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

public class ChargeWarningWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<ChargeWarningWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    
    public ChargeWarningWorker(AbpAsyncTimer timer, 
        ILogger<ChargeWarningWorker> logger, IClusterClient clusterClient, 
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.ChargeWarningTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessWarningCheckAsync();
    }

    private async Task ProcessWarningCheckAsync()
    {
        _logger.LogInformation("[ChargeWarningWorker] Process Charge Warning Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[ChargeWarningWorker] Check organization {0} {1}.", organizationName,
                organizationId);
            
        }
    }
}