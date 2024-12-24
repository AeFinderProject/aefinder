using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Market;
using AeFinder.Options;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class RenewalBalanceCheckWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly ILogger<AppInfoSyncWorker> _logger;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly GraphQLOptions _graphQlOptions;
    private readonly IAppDeployService _appDeployService;
    
    public RenewalBalanceCheckWorker(AbpAsyncTimer timer, ILogger<AppInfoSyncWorker> logger,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions,
        IAppDeployService appDeployService,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _organizationAppService = organizationAppService;
        _graphQlOptions = graphQlOptions.Value;
        _appDeployService = appDeployService;
        // Timer.Period = 24 * 60 * 60 * 1000; // 86400000 milliseconds = 24 hours
        Timer.Period = _scheduledTaskOptions.RenewalBalanceCheckTaskPeriodMilliSeconds;
    }
    
    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ProcessRenewalBalanceCheckAsync();
    }

    private async Task ProcessRenewalBalanceCheckAsync()
    {
        _logger.LogInformation("[RenewalBalanceCheckWorker] Process Renewal Balance Check Async.");
        var organizationUnitList = await _organizationAppService.GetAllOrganizationUnitsAsync();
        foreach (var organizationUnitDto in organizationUnitList)
        {
            var organizationId = organizationUnitDto.Id.ToString();
            var organizationName = organizationUnitDto.DisplayName;
            _logger.LogInformation("[RenewalBalanceCheckWorker] Check organization: {0}.", organizationId);
            var organizationGrainId = GetOrganizationGrainId(organizationUnitDto.Id);
            
            //Get organization account balance
            
            var renewalGrain = _clusterClient.GetGrain<IRenewalGrain>(organizationGrainId);
            var renewalList = await renewalGrain.GetAllActiveRenewalInfosAsync(organizationId);
            foreach (var renewalDto in renewalList)
            {
                if (renewalDto.AppId == _graphQlOptions.BillingIndexerId)
                {
                    var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(renewalDto.AppId));
                    var appInfo = await appGrain.GetAsync();
                    if (appInfo.Status == AppStatus.Frozen)
                    {
                        await _appDeployService.UnFreezeAppAsync(renewalDto.AppId);
                    }
                }
            }
        }
    }
    
    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }
}