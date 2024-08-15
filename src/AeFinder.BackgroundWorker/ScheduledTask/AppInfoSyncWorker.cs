using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.User;
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

public class AppInfoSyncWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppService _appService;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;

    public AppInfoSyncWorker(AbpAsyncTimer timer, IOrganizationAppService organizationAppService,
        ILogger<AppInfoSyncWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IAppService appService, IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IEntityMappingRepository<OrganizationIndex, string> organizationEntityMappingRepository,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _appService = appService;
        _organizationEntityMappingRepository = organizationEntityMappingRepository;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.AppInfoSyncTaskPeriodMilliSeconds;
    }
    
    // public override async Task StartAsync(CancellationToken cancellationToken)
    // {
    //     await base.StartAsync(cancellationToken);
    //     Timer.Start(cancellationToken);
    //     await ProcessSynchronizationAsync();
    // }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        _logger.LogInformation("[AppInfoSyncWorker] Process Synchronization Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            //Sync organization
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[AppInfoSyncWorker] Check organization: {0}.", organizationId);
            var organizationIndex = await _organizationEntityMappingRepository.GetAsync(organizationId);
            
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            var organizationAppGrain =
                _clusterClient.GetGrain<IOrganizationAppGrain>(organizationGrainId);
            var appIds = await organizationAppGrain.GetAppsAsync();
            
            if (organizationIndex == null)
            {
                _logger.LogWarning("[AppInfoSyncWorker] organization {0} is null.", organizationId);
                organizationIndex = new OrganizationIndex();
                organizationIndex.OrganizationId = organizationId;
                organizationIndex.OrganizationName = organizationName;
                organizationIndex.MaxAppCount = await _appService.GetMaxAppCountAsync(organizationUnitDto.Id);
                organizationIndex.AppIds = appIds.ToList();
                await _organizationEntityMappingRepository.AddOrUpdateAsync(organizationIndex);
                _logger.LogInformation(
                    "[AppInfoSyncWorker] Organization Synchronized: {0} max app count: {1} appids count: {2}.",
                    organizationIndex.OrganizationName, organizationIndex.MaxAppCount, organizationIndex.AppIds.Count);
            }

            foreach (var appId in appIds)
            {
                _logger.LogInformation("[AppInfoSyncWorker] Check appId: {0}.", appId);
                //Sync app info
                var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(appId);
                
                var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
                var appDto = await appGrain.GetAsync();
                var appName = appDto.AppName;
                var subscriptionGrain =
                    _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
                var versions = await subscriptionGrain.GetAllSubscriptionAsync();
                
                if (appInfoIndex == null)
                {
                    _logger.LogWarning("[AppInfoSyncWorker] app info {0} is null.", appId);
                    appInfoIndex = new AppInfoIndex();
                    appInfoIndex.AppId = appId;
                    appInfoIndex.OrganizationId = organizationId;
                    appInfoIndex.OrganizationName = organizationName;
                    appInfoIndex.AppName = appDto.AppName;
                    appInfoIndex.Description = appDto.Description;
                    appInfoIndex.ImageUrl = appDto.ImageUrl;
                    appInfoIndex.DeployKey = appDto.DeployKey;
                    appInfoIndex.SourceCodeUrl = appDto.SourceCodeUrl;
                    appInfoIndex.Status = appDto.Status;
                    DateTimeOffset createTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(appDto.CreateTime);
                    appInfoIndex.CreateTime = createTimeOffset.UtcDateTime;
                    DateTimeOffset updateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(appDto.UpdateTime);
                    appInfoIndex.UpdateTime = updateTimeOffset.UtcDateTime;
                    appInfoIndex.Versions = new AppVersionInfo()
                    {
                        CurrentVersion = versions.CurrentVersion?.Version,
                        PendingVersion = versions.PendingVersion?.Version
                    };
                    await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
                    _logger.LogInformation("[AppInfoSyncWorker] App info Synchronized: {0}.", appInfoIndex.AppName);
                }

                //Sync app limit info
                // var appLimitInfoIndex = await _appLimitInfoEntityMappingRepository.GetAsync(appId);
                // if (appLimitInfoIndex == null)
                // {
                //     _logger.LogWarning("[AppInfoSyncWorker] app limit info {0} is null.", appId);
                //     appLimitInfoIndex = new AppLimitInfoIndex();
                //     appLimitInfoIndex.AppId = appId;
                //     appLimitInfoIndex.OrganizationId = organizationId;
                //     appLimitInfoIndex.OrganizationName = organizationName;
                //     appLimitInfoIndex.AppName = appName;
                //     var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
                //         GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
                //     var resourceLimitDto = await appResourceLimitGrain.GetAsync();
                //     if (appLimitInfoIndex.ResourceLimit == null)
                //     {
                //         appLimitInfoIndex.ResourceLimit = new ResourceLimitInfo()
                //         {
                //             AppPodReplicas = resourceLimitDto.AppPodReplicas,
                //             AppFullPodRequestCpuCore = resourceLimitDto.AppFullPodRequestCpuCore,
                //             AppFullPodRequestMemory=resourceLimitDto.AppFullPodRequestMemory,
                //             AppQueryPodRequestCpuCore = resourceLimitDto.AppQueryPodRequestCpuCore,
                //             AppQueryPodRequestMemory = resourceLimitDto.AppQueryPodRequestMemory
                //         };
                //     }
                //
                //     if (appLimitInfoIndex.OperationLimit == null)
                //     {
                //         appLimitInfoIndex.OperationLimit = new OperationLimitInfo()
                //         {
                //             MaxEntityCallCount = resourceLimitDto.MaxEntityCallCount,
                //             MaxEntitySize = resourceLimitDto.MaxEntitySize,
                //             MaxLogCallCount = resourceLimitDto.MaxLogCallCount,
                //             MaxLogSize = resourceLimitDto.MaxLogSize,
                //             MaxContractCallCount = resourceLimitDto.MaxContractCallCount
                //         };
                //     }
                //     await _appLimitInfoEntityMappingRepository.AddOrUpdateAsync(appLimitInfoIndex);
                //     _logger.LogInformation("[AppInfoSyncWorker] App limit info Synchronized: {0}.", appLimitInfoIndex.AppName);
                // }

                if (versions == null)
                {
                    continue;
                }

                if (versions.CurrentVersion != null)
                {
                    var currentVersion = versions.CurrentVersion.Version;
                    //Sync current version subscription
                    var appSubscriptionIndex = await _appSubscriptionEntityMappingRepository.GetAsync(currentVersion);
                    if (appSubscriptionIndex == null)
                    {
                        appSubscriptionIndex = await CreateAppSubscriptionEntityAsync(appId, currentVersion);
                        await _appSubscriptionEntityMappingRepository.AddOrUpdateAsync(appSubscriptionIndex);
                        _logger.LogInformation("[AppInfoSyncWorker] App {0} currentVersion: {1} Synchronized.",
                            appSubscriptionIndex.AppId, currentVersion);
                    }
                }

                if (versions.PendingVersion != null)
                {
                    var pendingVersion = versions.PendingVersion.Version;
                    //Sync pending version subscription
                    var appSubscriptionIndex = await _appSubscriptionEntityMappingRepository.GetAsync(pendingVersion);
                    if (appSubscriptionIndex == null)
                    {
                        appSubscriptionIndex = await CreateAppSubscriptionEntityAsync(appId, pendingVersion);
                        await _appSubscriptionEntityMappingRepository.AddOrUpdateAsync(appSubscriptionIndex);
                        _logger.LogInformation("[AppInfoSyncWorker] App {0} pendingVersion: {1} Synchronized.",
                            appSubscriptionIndex.AppId, pendingVersion);
                    }
                }
            }

        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }

    private async Task<AppSubscriptionIndex> CreateAppSubscriptionEntityAsync(string appId, string version)
    {
        var appSubscriptionIndex = new AppSubscriptionIndex()
        {
            AppId = appId,
            Version = version
        };
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var subscriptionManifest = await appSubscriptionGrain.GetSubscriptionAsync(version);
        var subscriptionStatus = await appSubscriptionGrain.GetSubscriptionStatusAsync(version);
        var subscriptionManifestInfo =
            _objectMapper.Map<SubscriptionManifest, SubscriptionManifestInfo>(subscriptionManifest);
        appSubscriptionIndex.SubscriptionManifest = subscriptionManifestInfo;
        appSubscriptionIndex.SubscriptionStatus = subscriptionStatus;
        return appSubscriptionIndex;
    }
}