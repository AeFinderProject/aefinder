using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppCreateHandler : AppHandlerBase, IDistributedEventHandler<AppCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;

    public AppCreateHandler(IEntityMappingRepository<OrganizationIndex, string> organizationEntityMappingRepository,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository,
        IOrganizationAppService organizationAppService, IAppResourceLimitProvider appResourceLimitProvider)
    {
        _organizationEntityMappingRepository = organizationEntityMappingRepository;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
        _organizationAppService = organizationAppService;
        _appResourceLimitProvider = appResourceLimitProvider;
    }

    public async Task HandleEventAsync(AppCreateEto eventData)
    {
        //Update organization app ids
        var organizationIndex = await _organizationEntityMappingRepository.GetAsync(eventData.OrganizationId);
        if (organizationIndex == null || organizationIndex.OrganizationId.IsNullOrEmpty())
        {
            Logger.LogError($"Organization {eventData.OrganizationId} info is missing.");
            organizationIndex = new OrganizationIndex();
            organizationIndex.OrganizationId = eventData.OrganizationId;
            Guid organizationUnitGuid;
            if (!Guid.TryParse(eventData.OrganizationId, out organizationUnitGuid))
            {
                throw new Exception($"Invalid OrganizationUnitId string: {eventData.OrganizationId}");
            }

            var organizationUnitDto= await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);
            organizationIndex.OrganizationName = organizationUnitDto.DisplayName;
        }

        if (organizationIndex.AppIds == null)
        {
            organizationIndex.AppIds = new List<string>();
        }

        if (!organizationIndex.AppIds.Contains(eventData.AppId))
        {
            organizationIndex.AppIds.Add(eventData.AppId);
        }

        await _organizationEntityMappingRepository.AddOrUpdateAsync(organizationIndex);

        //Add app info index
        var appInfoIndex = ObjectMapper.Map<AppCreateEto, AppInfoIndex>(eventData);
        appInfoIndex.OrganizationName = organizationIndex.OrganizationName;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
        
        //Add app resource limit info index
        var appLimitInfoIndex = ObjectMapper.Map<AppCreateEto, AppLimitInfoIndex>(eventData);
        appLimitInfoIndex.OrganizationName = organizationIndex.OrganizationName;
        var appResourceLimitDto = await _appResourceLimitProvider.GetAppResourceLimitAsync(eventData.AppId);
        appLimitInfoIndex.ResourceLimit = new ResourceLimitInfo();
        appLimitInfoIndex.ResourceLimit.AppFullPodRequestCpuCore = appResourceLimitDto.AppFullPodRequestCpuCore;
        appLimitInfoIndex.ResourceLimit.AppFullPodRequestMemory = appResourceLimitDto.AppFullPodRequestMemory;
        appLimitInfoIndex.ResourceLimit.AppQueryPodRequestCpuCore = appResourceLimitDto.AppQueryPodRequestCpuCore;
        appLimitInfoIndex.ResourceLimit.AppQueryPodRequestMemory = appResourceLimitDto.AppQueryPodRequestMemory;
        appLimitInfoIndex.ResourceLimit.AppPodReplicas = appResourceLimitDto.AppPodReplicas;
        appLimitInfoIndex.OperationLimit = new OperationLimitInfo();
        appLimitInfoIndex.OperationLimit.MaxEntityCallCount = appResourceLimitDto.MaxEntityCallCount;
        appLimitInfoIndex.OperationLimit.MaxEntitySize = appResourceLimitDto.MaxEntitySize;
        appLimitInfoIndex.OperationLimit.MaxLogCallCount = appResourceLimitDto.MaxLogCallCount;
        appLimitInfoIndex.OperationLimit.MaxLogSize = appResourceLimitDto.MaxLogSize;
        appLimitInfoIndex.OperationLimit.MaxContractCallCount = appResourceLimitDto.MaxContractCallCount;
        await _appLimitInfoEntityMappingRepository.AddOrUpdateAsync(appLimitInfoIndex);
    }
}