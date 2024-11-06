using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Metrics;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class AppPodResourceInfoSyncWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppPodResourceInfoSyncWorker> _logger;
    private readonly IAppDeployService _appDeployService;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IKubernetesAppMonitor _kubernetesAppMonitor;
    private readonly IObjectMapper _objectMapper;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    
    public AppPodResourceInfoSyncWorker(AbpAsyncTimer timer,ILogger<AppPodResourceInfoSyncWorker> logger, 
        IKubernetesAppMonitor kubernetesAppMonitor,IObjectMapper objectMapper,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,IAppDeployService appDeployService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appDeployService = appDeployService;
        _kubernetesAppMonitor = kubernetesAppMonitor;
        _objectMapper = objectMapper;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
        // Timer.Period = 10 * 60 * 1000; // 600000 milliseconds = 10 minutes
        Timer.Period = _scheduledTaskOptions.AppPodListSyncTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        //get pods from es by page
        
        
        // var podNames = podsPageResultDto.PodInfos.Select(p => p.PodName).ToList();
        // var prometheusPodsInfo = await _kubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podNames);
        // //TODO convert prometheusPodsInfo to 
    }
}