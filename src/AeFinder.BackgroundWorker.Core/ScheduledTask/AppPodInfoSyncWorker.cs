using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.BackgroundWorker.Options;
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

public class AppPodInfoSyncWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppPodInfoSyncWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IAppDeployService _appDeployService;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    private readonly int syncPageSize = 50;
    
    public AppPodInfoSyncWorker(AbpAsyncTimer timer,
        ILogger<AppPodInfoSyncWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IAppDeployService appDeployService,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appDeployService = appDeployService;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.AppPodInfoSyncTaskPeriodMilliSeconds;
    }
    
    
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        _logger.LogInformation("[AppPodInfoSyncWorker] Process Synchronization Async.");
        //Todo Delete all old app pod index data

        //Start process synchronization
        // string continueToken = null;
        //
        // do
        // {
        //     var podsPageResultDto = await _appDeployService.GetPodListWithPagingAsync(syncPageSize, continueToken);
        //     continueToken = podsPageResultDto.ContinueToken;
        //     
        //     var podsInfoIndexList =
        //         _objectMapper.Map<List<AppPodInfoDto>, List<AppPodInfoIndex>>(podsPageResultDto.PodInfos);
        //     await _appPodInfoEntityMappingRepository.AddOrUpdateManyAsync(podsInfoIndexList);
        //
        // } while (!string.IsNullOrEmpty(continueToken));
    }
}