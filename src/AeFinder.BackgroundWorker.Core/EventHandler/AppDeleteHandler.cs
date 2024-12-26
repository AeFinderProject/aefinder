using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppDeleteHandler : IDistributedEventHandler<AppDeleteEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;

    public AppDeleteHandler(IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository)
    {
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
    }
    
    public async Task HandleEventAsync(AppDeleteEto eventData)
    {
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(eventData.AppId);
        appInfoIndex.Status = eventData.Status;
        appInfoIndex.DeleteTime = eventData.DeleteTime;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
    }
}