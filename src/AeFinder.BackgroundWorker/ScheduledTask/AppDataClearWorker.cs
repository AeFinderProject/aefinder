using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class AppDataClearWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppDataClearWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IMongoDbService _mongoDbService;
    private readonly OrleansDataClearOptions _orleansDataClearOptions;

    public AppDataClearWorker(AbpTimer timer, IMongoDbService mongoDbService,
        ILogger<AppDataClearWorker> logger, IClusterClient clusterClient,
        IOptionsSnapshot<OrleansDataClearOptions> mongoOrleansDbOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _mongoDbService = mongoDbService;
        _orleansDataClearOptions = mongoOrleansDbOptions.Value;
        Timer.Period = _orleansDataClearOptions.ClearTaskPeriodMilliSeconds; // 180000 milliseconds = 3 minutes
    }

    [UnitOfWork]
    protected override void DoWork(PeriodicBackgroundWorkerContext workerContext)
    {
        AsyncHelper.RunSync(() => ProcessDeletionAsync());
    }
    
    private async Task ProcessDeletionAsync()
    {
        var appDataClearManagerGrain =
            _clusterClient.GetGrain<IAppDataClearManagerGrain>(GrainIdHelper.GenerateAppDataClearManagerGrainId());

        var versionClearTasks = await appDataClearManagerGrain.GetVersionClearTasksAsync();
        _logger.LogInformation("[AppDataClearWorker] Process Deletion Async, Task count: {0}", versionClearTasks.Count);
        if (versionClearTasks.Count == 0)
        {
            return;
        }

        var taskInfo = versionClearTasks.First();
        var appId = taskInfo.Value;
        var version = taskInfo.Key;
        var limitCount = _orleansDataClearOptions.PeriodClearLimitCount;

        //remove AppBlockStateChangeGrain grain data
        var appBlockStateChangeGrainCollectionName = OrleansConstants.GrainCollectionPrefix + typeof(AppBlockStateChangeGrain).Name;
        var appBlockStateChangeGrainIdPrefix =
            $"{_orleansDataClearOptions.AppBlockStateChangeGrainIdPrefix}+{appId}-{version}";
        var appBlockStateChangeGrainDeleteIdList =
            await _mongoDbService.QueryRecordIdsWithPrefixAsync(appBlockStateChangeGrainCollectionName, appBlockStateChangeGrainIdPrefix, limitCount);
        _logger.LogInformation(
            $"[GrainDataClearWorker]Start clear AppBlockStateChangeGrain recordCount:{appBlockStateChangeGrainDeleteIdList.Count} CollectionName: {appBlockStateChangeGrainCollectionName} IdPrefix: {appBlockStateChangeGrainIdPrefix}");
        
        var appBlockStateChangeGrainDeletedCount = 0L;
        if (appBlockStateChangeGrainDeleteIdList.Count > 0)
        {
            appBlockStateChangeGrainDeletedCount =
                await _mongoDbService.DeleteRecordsWithIdsAsync(appBlockStateChangeGrainCollectionName,
                    appBlockStateChangeGrainDeleteIdList);
        }
        
        //remove AppStateGrain grain data
        var appStateGrainCollectionName = OrleansConstants.GrainCollectionPrefix + typeof(AppStateGrain).Name;
        var appStateGrainIdPrefix =
            $"{_orleansDataClearOptions.AppStateGrainIdPrefix}+{appId}-{version}";
        var appStateGrainDeleteIdList =
            await _mongoDbService.QueryRecordIdsWithPrefixAsync(appStateGrainCollectionName, appStateGrainIdPrefix,
                limitCount);
        _logger.LogInformation(
            $"[GrainDataClearWorker]Start clear AppStateGrain recordCount:{appStateGrainDeleteIdList.Count} CollectionName: {appStateGrainCollectionName} IdPrefix: {appStateGrainIdPrefix}");
        
        var appStateGrainDeletedCount = 0L;
        if (appStateGrainDeleteIdList.Count > 0)
        {
            appStateGrainDeletedCount =
                await _mongoDbService.DeleteRecordsWithIdsAsync(appStateGrainCollectionName, appStateGrainDeleteIdList);
        }

        //only both AppBlockStateChangeGrain & AppStateGrain all cleared can do remove
        if (appBlockStateChangeGrainDeleteIdList.Count == 0 && appStateGrainDeleteIdList.Count == 0)
        {
            await appDataClearManagerGrain.RemoveVersionClearTaskAsync(version);
            _logger.LogInformation($"[GrainDataClearWorker]Task {version} removed");
            return;
        }
    }
}