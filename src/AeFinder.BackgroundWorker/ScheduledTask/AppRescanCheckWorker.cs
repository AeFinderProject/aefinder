using System.Linq.Dynamic.Core;
using AeFinder.App.Es;
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
    
    public AppRescanCheckWorker(AbpAsyncTimer timer, IEntityMappingRepository<AppInfoIndex, string> appIndexRepository,
        ILogger<AppRescanCheckWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appIndexRepository = appIndexRepository;
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
                var appSubscriptionGrain =
                    _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
                var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();
                if (allSubscription.CurrentVersion != null)
                {
                    if (allSubscription.CurrentVersion.Status == SubscriptionStatus.Started)
                    {
                        
                    }
                }

                if (allSubscription.PendingVersion != null)
                {
                    if (allSubscription.PendingVersion.Status == SubscriptionStatus.Paused)
                    {
                        continue;
                    }
                    
                }
            }
            
            skipCount = skipCount + maxResultCount;
        }
    }
}