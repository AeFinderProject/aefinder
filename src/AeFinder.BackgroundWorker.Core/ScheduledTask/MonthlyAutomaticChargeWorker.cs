using AeFinder.BackgroundWorker.Options;
using AeFinder.Billings;
using AeFinder.Grains;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class MonthlyAutomaticChargeWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<MonthlyAutomaticChargeWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IBillingManagementService _billingManagementService;
    private readonly IClock _clock;

    public MonthlyAutomaticChargeWorker(AbpAsyncTimer timer,
        ILogger<MonthlyAutomaticChargeWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOrganizationAppService organizationAppService,
        IServiceScopeFactory serviceScopeFactory, IBillingManagementService billingManagementService,
        IClock clock) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _billingManagementService = billingManagementService;
        _clock = clock;
        Timer.Period = _scheduledTaskOptions.MonthlyAutomaticChargeTaskPeriodMilliSeconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (_clock.Now.Day >= _scheduledTaskOptions.MonthlyAutomaticChargeDay)
        {
            return;
        }

        _logger.LogInformation("Handle monthly billing.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var firstDayOfThisMonth = _clock.Now.ToMonthDate();
            if (organizationUnitDto.CreationTime >= firstDayOfThisMonth)
            {
                continue;
            }

            await _billingManagementService.GenerateMonthlyBillingAsync(organizationUnitDto.Id,
                _clock.Now.AddMonths(-1));
        }
    }
}