using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
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

public class AppRescanCheckWorker: AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppRescanCheckWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appIndexRepository;
    private readonly IAppDeployService _appDeployService;
    private readonly Dictionary<string, int> _subscriptionRescanTimes = new();
    
    public AppRescanCheckWorker(AbpAsyncTimer timer, IEntityMappingRepository<AppInfoIndex, string> appIndexRepository,
        ILogger<AppRescanCheckWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,IAppDeployService appDeployService,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appIndexRepository = appIndexRepository;
        _appDeployService = appDeployService;
        // Timer.Period = 10 * 60 * 1000; // 600000 milliseconds = 10 minutes
        Timer.Period = _scheduledTaskOptions.AppRescanCheckTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRescanCheckAsync();
    }

    public async Task ProcessRescanCheckAsync()
    {
        _logger.LogInformation("[AppRescanCheckWorker] Process rescan check.");
        var skipCount = 0;
        var maxResultCount = 20;
        while (true)
        {
            var queryable = await _appIndexRepository.GetQueryableAsync();
            var apps = queryable.OrderBy(o => o.AppName).Skip(skipCount).Take(maxResultCount).ToList();

            if (apps == null || apps.Count == 0)
            {
                return;
            }

            foreach (var appInfo in apps)
            {
                var appId = appInfo.AppId;
                if (appInfo.Versions == null)
                {
                    continue;
                }
                
                if (!appInfo.Versions.CurrentVersion.IsNullOrEmpty())
                {
                    await RescanSubscriptionAsync(appId, appInfo.Versions.CurrentVersion);
                }

                if (!appInfo.Versions.PendingVersion.IsNullOrEmpty())
                {
                    await RescanSubscriptionAsync(appId, appInfo.Versions.PendingVersion);
                }
            }
            
            skipCount = skipCount + maxResultCount;
        }
    }

    private async Task RescanSubscriptionAsync(string appId, string version)
    {
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscriptionStatus = await appSubscriptionGrain.GetSubscriptionStatusAsync(version);
        if (subscriptionStatus == SubscriptionStatus.Paused)
        {
            return;
        }
        
        if (!_subscriptionRescanTimes.ContainsKey(version))
        {
            _subscriptionRescanTimes.Add(version, 0);
        }
        var appSubscriptionProcessingStatusGrain =
            _clusterClient.GetGrain<IAppSubscriptionProcessingStatusGrain>(
                GrainIdHelper.GenerateAppSubscriptionProcessingStatusGrainId(appId, version));

        var processingStatusDictionary = await appSubscriptionProcessingStatusGrain.GetProcessingStatusAsync();
        var isProcessingFailed = await IsAppProcessingFailedAsync(processingStatusDictionary);
        if (isProcessingFailed)
        {
            if (_subscriptionRescanTimes[version] < _scheduledTaskOptions.MaxAppRescanTimes)
            {
                _subscriptionRescanTimes[version] =
                    _subscriptionRescanTimes[version] + 1;
                _logger.LogInformation(
                    $"[AppRescanCheckWorker] Detected App {appId} processing failed in version {version}, immediately attempting to restart {_subscriptionRescanTimes[version]}");
                await _appDeployService.RestartAppAsync(appId, version);
            }
        }
        else
        {
            _subscriptionRescanTimes[version] = 0;
        }
    }
    
    private async Task<bool> IsAppProcessingFailedAsync(ConcurrentDictionary<string,ProcessingStatus> processingStatusDictionary)
    {
        if (processingStatusDictionary == null)
        {
            return false;
        }
        
        foreach (var processingStatusKeyValuePair in processingStatusDictionary)
        {
            var chainId = processingStatusKeyValuePair.Key;
            if (processingStatusKeyValuePair.Value == ProcessingStatus.Failed)
            {
                return true;
            }
        }

        return false;
    }
}