using System;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.ScheduledTask;

public class AppExtensionInfoSyncWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppExtensionInfoSyncWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppResourceLimitProvider _limitProvider;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;

    public AppExtensionInfoSyncWorker(AbpAsyncTimer timer, IOrganizationAppService organizationAppService,
        ILogger<AppExtensionInfoSyncWorker> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IAppResourceLimitProvider limitProvider, IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _limitProvider = limitProvider;
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.AppInfoSyncTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessSynchronizationAsync();
    }

    private async Task ProcessSynchronizationAsync()
    {
        _logger.LogInformation("[AppExtensionInfoSyncWorker] Process Synchronization Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[AppExtensionInfoSyncWorker] Check organization: {0}.", organizationId);
            
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            var organizationAppGrain =
                _clusterClient.GetGrain<IOrganizationAppGrain>(organizationGrainId);
            var appIds = await organizationAppGrain.GetAppsAsync();

            foreach (var appId in appIds)
            {
                _logger.LogInformation("[AppExtensionInfoSyncWorker] Check appId: {0}.", appId);
                var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
                var appDto = await appGrain.GetAsync();
                var appName = appDto.AppName;
                //Sync app limit info
                var appLimitInfoIndex = await _appLimitInfoEntityMappingRepository.GetAsync(appId);
                if (appLimitInfoIndex == null)
                {
                    _logger.LogWarning("[AppExtensionInfoSyncWorker] app limit info {0} is null.", appId);
                    appLimitInfoIndex = new AppLimitInfoIndex();
                    appLimitInfoIndex.AppId = appId;
                    appLimitInfoIndex.OrganizationId = organizationId;
                    appLimitInfoIndex.OrganizationName = organizationName;
                    appLimitInfoIndex.AppName = appName;

                    var resourceLimitDto = await _limitProvider.GetAppResourceLimitAsync(appId);
                    appLimitInfoIndex.ResourceLimit = new ResourceLimitInfo()
                    {
                        AppPodReplicas = resourceLimitDto.AppPodReplicas,
                        AppFullPodRequestCpuCore = resourceLimitDto.AppFullPodRequestCpuCore,
                        AppFullPodRequestMemory=resourceLimitDto.AppFullPodRequestMemory,
                        AppQueryPodRequestCpuCore = resourceLimitDto.AppQueryPodRequestCpuCore,
                        AppQueryPodRequestMemory = resourceLimitDto.AppQueryPodRequestMemory
                    };
                
                    appLimitInfoIndex.OperationLimit = new OperationLimitInfo()
                    {
                        MaxEntityCallCount = resourceLimitDto.MaxEntityCallCount,
                        MaxEntitySize = resourceLimitDto.MaxEntitySize,
                        MaxLogCallCount = resourceLimitDto.MaxLogCallCount,
                        MaxLogSize = resourceLimitDto.MaxLogSize,
                        MaxContractCallCount = resourceLimitDto.MaxContractCallCount
                    };

                    appLimitInfoIndex.DeployLimit = new DeployLimitInfo()
                    {
                        MaxAppCodeSize = resourceLimitDto.MaxAppCodeSize,
                        MaxAppAttachmentSize = resourceLimitDto.MaxAppAttachmentSize
                    };
                    await _appLimitInfoEntityMappingRepository.AddOrUpdateAsync(appLimitInfoIndex);
                    _logger.LogInformation("[AppExtensionInfoSyncWorker] App limit info Synchronized: {0}.", appLimitInfoIndex.AppName);
                }
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }
}