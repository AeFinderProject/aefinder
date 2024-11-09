using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
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

public class AppPodListSyncWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppPodListSyncWorker> _logger;
    private readonly IAppDeployService _appDeployService;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IKubernetesAppMonitor _kubernetesAppMonitor;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    
    public AppPodListSyncWorker(AbpAsyncTimer timer,ILogger<AppPodListSyncWorker> logger, 
        IKubernetesAppMonitor kubernetesAppMonitor,IObjectMapper objectMapper,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,IAppDeployService appDeployService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appDeployService = appDeployService;
        _objectMapper = objectMapper;
        _kubernetesAppMonitor = kubernetesAppMonitor;
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
        int pageSize = 10;
        string continueToken = null;
        _logger.LogInformation($"[AppPodListSyncWorker] Start Process Synchronization Async. PageSize:{pageSize}");
        //Delete old pod info record first
        var oldPodIndexList = await _appPodInfoEntityMappingRepository.GetListAsync();
        if (oldPodIndexList.Count > 0)
        {
            await _appPodInfoEntityMappingRepository.DeleteManyAsync(oldPodIndexList);
        }
        _logger.LogInformation($"[AppPodListSyncWorker] All old pod info indexes deleted.");
        do
        {
            var podsPageResultDto = await _appDeployService.GetPodListWithPagingAsync(null, pageSize, continueToken);
            continueToken = podsPageResultDto.ContinueToken;
            if (podsPageResultDto.PodInfos.Count > 0)
            {
                var appPodInfoIndexList =
                    _objectMapper.Map<List<AppPodInfoDto>, List<AppPodInfoIndex>>(podsPageResultDto.PodInfos);
                
                //Query resource usage info
                var podNames = appPodInfoIndexList.Select(p => p.PodName).ToList();
                var prometheusPodsInfo = await _kubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podNames);
                //Update pod resource usage
                foreach (var podInfoIndex in appPodInfoIndexList)
                {
                    var prometheusPodInfo = prometheusPodsInfo.FirstOrDefault(p => p.PodName == podInfoIndex.PodName);
                    if (prometheusPodInfo == null)
                    {
                        _logger.LogInformation($"[AppPodListSyncWorker]Pod {podInfoIndex.PodName} not found.");
                        continue;
                    }

                    podInfoIndex.UsageTimestamp = prometheusPodInfo.Timestamp;
                    podInfoIndex.CpuUsage = prometheusPodInfo.CpuUsage;
                    podInfoIndex.MemoryUsage = prometheusPodInfo.MemoryUsage;

                    foreach (var containerInfo in podInfoIndex.Containers)
                    {
                        var prometheusContainerInfo =
                            prometheusPodInfo.Containers.FirstOrDefault(c =>
                                c.ContainerName == containerInfo.ContainerName);
                        if (prometheusContainerInfo == null)
                        {
                            _logger.LogInformation($"[AppPodListSyncWorker]Container {containerInfo.ContainerName} not found.");
                            continue;
                        }

                        containerInfo.UsageTimestamp = prometheusContainerInfo.Timestamp;
                        containerInfo.CpuUsage = prometheusContainerInfo.CpuUsage;
                        containerInfo.MemoryUsage = prometheusContainerInfo.MemoryUsage;
                    }
                }
                
                //Add new pod info record
                await _appPodInfoEntityMappingRepository.AddOrUpdateManyAsync(appPodInfoIndexList);
            }
            
        } while (!continueToken.IsNullOrEmpty());
        _logger.LogInformation($"[AppPodListSyncWorker] Process Synchronization Completion.");
    }
}