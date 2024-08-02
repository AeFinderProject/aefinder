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

public class GrainDataClearWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<GrainDataClearWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IMongoDbService _mongoDbService;
    private readonly MongoOrleansDbOptions _mongoOrleansDbOptions;

    public GrainDataClearWorker(AbpTimer timer, IMongoDbService mongoDbService,
        ILogger<GrainDataClearWorker> logger, IClusterClient clusterClient,
        IOptionsSnapshot<MongoOrleansDbOptions> mongoOrleansDbOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _mongoDbService = mongoDbService;
        _mongoOrleansDbOptions = mongoOrleansDbOptions.Value;
        Timer.Period = _mongoOrleansDbOptions.ClearTaskPeriodMilliSeconds; // 180000 milliseconds = 3 minutes
    }

    [UnitOfWork]
    protected override void DoWork(PeriodicBackgroundWorkerContext workerContext)
    {
        AsyncHelper.RunSync(() => ProcessDeletion());
    }
    
    private async Task ProcessDeletion()
    {
        var appDataClearManagerGrain =
            _clusterClient.GetGrain<IAppDataClearManagerGrain>(GrainIdHelper.GenerateAppDataClearManagerGrainId());

        var versionClearTasks = await appDataClearManagerGrain.GetVersionClearTasksAsync();

        if (versionClearTasks.Count == 0)
        {
            return;
        }

        var taskInfo = versionClearTasks.FirstOrDefault();
        var appId = taskInfo.Value;
        var version = taskInfo.Key;
        var limitCount = _mongoOrleansDbOptions.PeriodClearLimitCount;

        //remove AppBlockStateChangeGrain grain data
        var appBlockStateChangeGrainCollectionName = OrleansConstants.GrainCollectionPrefix + typeof(AppBlockStateChangeGrain).Name;
        var appBlockStateChangeGrainIdPrefix =
            $"{_mongoOrleansDbOptions.AppBlockStateChangeGrainIdPrefix}+{appId}-{version}";
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
            $"{_mongoOrleansDbOptions.AppStateGrainIdPrefix}+{appId}-{version}";
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
            _logger.LogInformation("$[GrainDataClearWorker]Task {version} removed");
            return;
        }
    }
}