using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppLimitUpdateHandler : AppHandlerBase, IDistributedEventHandler<AppLimitUpdateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;

    public AppLimitUpdateHandler(IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
        _organizationAppService = organizationAppService;
    }


    public async Task HandleEventAsync(AppLimitUpdateEto eventData)
    {
        //Add app resource limit info index
        var appLimitInfoIndex = new AppLimitInfoIndex();
        appLimitInfoIndex.AppId = eventData.AppId;
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(eventData.AppId));
        var appDto = await appGrain.GetAsync();

        var organizationId = await appGrain.GetOrganizationIdAsync();
        Guid organizationUnitGuid;
        if (!Guid.TryParse(organizationId, out organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {organizationId}");
        }

        var organizationUnitDto = await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);

        appLimitInfoIndex.AppName = appDto.AppName;
        appLimitInfoIndex.OrganizationId = organizationId;
        appLimitInfoIndex.OrganizationName = organizationUnitDto.DisplayName;
        appLimitInfoIndex.ResourceLimit = new ResourceLimitInfo();
        appLimitInfoIndex.ResourceLimit.AppFullPodRequestCpuCore = eventData.AppFullPodRequestCpuCore;
        appLimitInfoIndex.ResourceLimit.AppFullPodRequestMemory = eventData.AppFullPodRequestMemory;
        appLimitInfoIndex.ResourceLimit.AppQueryPodRequestCpuCore = eventData.AppQueryPodRequestCpuCore;
        appLimitInfoIndex.ResourceLimit.AppQueryPodRequestMemory = eventData.AppQueryPodRequestMemory;
        appLimitInfoIndex.ResourceLimit.AppPodReplicas = eventData.AppPodReplicas;
        appLimitInfoIndex.OperationLimit = new OperationLimitInfo();
        appLimitInfoIndex.OperationLimit.MaxEntityCallCount = eventData.MaxEntityCallCount;
        appLimitInfoIndex.OperationLimit.MaxEntitySize = eventData.MaxEntitySize;
        appLimitInfoIndex.OperationLimit.MaxLogCallCount = eventData.MaxLogCallCount;
        appLimitInfoIndex.OperationLimit.MaxLogSize = eventData.MaxLogSize;
        appLimitInfoIndex.OperationLimit.MaxContractCallCount = eventData.MaxContractCallCount;
        appLimitInfoIndex.DeployLimit = new DeployLimitInfo();
        appLimitInfoIndex.DeployLimit.MaxAppCodeSize = eventData.MaxAppCodeSize;
        appLimitInfoIndex.DeployLimit.MaxAppAttachmentSize = eventData.MaxAppAttachmentSize;
        await _appLimitInfoEntityMappingRepository.AddOrUpdateAsync(appLimitInfoIndex);
    }
}