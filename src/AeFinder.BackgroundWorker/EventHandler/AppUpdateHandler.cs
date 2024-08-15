using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpdateHandler : AppHandlerBase, IDistributedEventHandler<AppUpdateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    
    public AppUpdateHandler(
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository)
    {
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
    }
    
    public async Task HandleEventAsync(AppUpdateEto eventData)
    {
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(eventData.AppId);
        if (appInfoIndex == null)
        {
            Logger.LogError($"[AppUpdateHandler]App {eventData.AppId} info is missing.");
            appInfoIndex = new AppInfoIndex();
            appInfoIndex.AppId = eventData.AppId;
        }

        appInfoIndex.Description = eventData.Description;
        appInfoIndex.ImageUrl = eventData.ImageUrl;
        appInfoIndex.SourceCodeUrl = eventData.SourceCodeUrl;
        appInfoIndex.UpdateTime = eventData.UpdateTime;

        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
    }
}