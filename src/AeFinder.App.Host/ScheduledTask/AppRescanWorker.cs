using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.App.ScheduledTask;

public class AppRescanWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppRescanWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    
    public AppRescanWorker(AbpAsyncTimer timer, 
        ILogger<AppRescanWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        // Timer.Period = 10 * 60 * 1000; // 600000 milliseconds = 10 minutes
        Timer.Period = _scheduledTaskOptions.AppRescanCheckTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRescanCheckAsync();
    }

    private async Task ProcessRescanCheckAsync()
    {
        
    }
}