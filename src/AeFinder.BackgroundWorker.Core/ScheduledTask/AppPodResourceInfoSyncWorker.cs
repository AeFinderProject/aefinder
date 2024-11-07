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
        // Timer.Period = 5 * 60 * 1000; // 300000 milliseconds = 5 minutes
        Timer.Period = _scheduledTaskOptions.AppPodResourceSyncTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        int pageSize = 10;
        int skipCount = 0;
        bool continueProcessing = true;
        _logger.LogInformation($"[AppPodResourceInfoSyncWorker] Start Process Synchronization Async. PageSize:{pageSize}");
        while (continueProcessing)
        {
            //get pods from es by page
            var queryable = await _appPodInfoEntityMappingRepository.GetQueryableAsync();
            queryable = queryable.OrderBy(o => o.PodName).Skip(skipCount).Take(pageSize);
            var podInfoIndexList = queryable.ToList();
            if (podInfoIndexList.Count == 0)
            {
                continueProcessing = false;
                break;
            }

            var podNames = podInfoIndexList.Select(p => p.PodName).ToList();
            var prometheusPodsInfo = await _kubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podNames);
            //Update pod resource usage
            foreach (var podInfoIndex in podInfoIndexList)
            {
                var prometheusInfo = prometheusPodsInfo.FirstOrDefault(p => p.PodName == podInfoIndex.PodName);
                if (prometheusInfo == null)
                {
                    _logger.LogInformation($"[AppPodResourceInfoSyncWorker]Pod {podInfoIndex.PodName} not found.");
                    continue;
                }

                foreach (var containerInfo in podInfoIndex.Containers)
                {
                    containerInfo.CpuUsage = "";
                    containerInfo.MemoryUsage = "";
                }
            }

            await _appPodInfoEntityMappingRepository.UpdateManyAsync(podInfoIndexList);

            skipCount += pageSize;
            if (podInfoIndexList.Count < pageSize)
            {
                continueProcessing = false;
            }
        }
        
        _logger.LogInformation("[AppPodResourceInfoSyncWorker] Process Synchronization Completion.");
    }
}