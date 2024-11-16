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

public class AppPodResourceInfoSyncWorker: AppPodInfoSyncWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppPodResourceInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IKubernetesAppMonitor _kubernetesAppMonitor;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    
    public AppPodResourceInfoSyncWorker(AbpAsyncTimer timer,ILogger<AppPodResourceInfoSyncWorker> logger, 
        IKubernetesAppMonitor kubernetesAppMonitor,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory, logger, kubernetesAppMonitor)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _kubernetesAppMonitor = kubernetesAppMonitor;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
        // Timer.Period = 3 * 60 * 1000; // 180000 milliseconds = 3 minutes
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

            //Update pod resource usage info
            podInfoIndexList = await UpdatePodResourceUsageAsync(podInfoIndexList);

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