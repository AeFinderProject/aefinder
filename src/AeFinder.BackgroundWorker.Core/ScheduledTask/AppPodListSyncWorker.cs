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
    private readonly IKubernetesAppMonitor _kubernetesAppMonitor;
    private readonly IObjectMapper _objectMapper;
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
        int pageSize = 10;
        var podsPageResultDto = await _appDeployService.GetPodListWithPagingAsync(null, pageSize, null);
        var appPodInfoIndexList =
            _objectMapper.Map<List<AppPodInfoDto>, List<AppPodInfoIndex>>(podsPageResultDto.PodInfos);
        //Delete old pod info record first
        foreach (var podInfo in appPodInfoIndexList)
        {
            var oldPodIndexList = await _appPodInfoEntityMappingRepository.GetListAsync(p =>
                p.AppId == podInfo.AppId && p.AppVersion == podInfo.AppVersion);
            if (oldPodIndexList.Count > 0)
            {
                await _appPodInfoEntityMappingRepository.DeleteManyAsync(oldPodIndexList);
            }
        }
        
        //Add new pod info record
        await _appPodInfoEntityMappingRepository.AddOrUpdateManyAsync(appPodInfoIndexList);
    }
}