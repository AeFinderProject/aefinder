using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppLimitUpdateHandler : AppHandlerBase, IDistributedEventHandler<AppLimitUpdateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;

    public AppLimitUpdateHandler(
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository)
    {
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
    }
    
    
    public async Task HandleEventAsync(AppLimitUpdateEto eventData)
    {
        //Add app resource limit info index
        var appLimitInfoIndex = await _appLimitInfoEntityMappingRepository.GetAsync(eventData.AppId);
        if (appLimitInfoIndex == null)
        {
            appLimitInfoIndex = new AppLimitInfoIndex();
            appLimitInfoIndex.AppId = eventData.AppId;
        }
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
        await _appLimitInfoEntityMappingRepository.AddOrUpdateAsync(appLimitInfoIndex);
    }
}