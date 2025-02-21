using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Assets;
using AeFinder.BlockScan;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Merchandises;
using AeFinder.User.Provider;
using AElf.Client.Dto;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppDeleteHandler : IDistributedEventHandler<AppDeleteEto>, ITransientDependency
{
    private readonly ILogger<AppDeleteHandler> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IAssetService _assetService;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;

    public AppDeleteHandler(ILogger<AppDeleteHandler> logger,
        IClusterClient clusterClient,
        IBlockScanAppService blockScanAppService,
        IAssetService assetService,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<OrganizationIndex, string> organizationEntityMappingRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
        _assetService = assetService;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _organizationEntityMappingRepository = organizationEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppDeleteEto eventData)
    {
        var appId = eventData.AppId;
        var organizationId = eventData.OrganizationId;
        var organizationGuid = Guid.Parse(organizationId);
        //Release app all asset
        var assets = await _assetService.GetListAsync(organizationGuid, new GetAssetInput()
        {
            Category = MerchandiseCategory.Resource,
            AppId = appId,
            SkipCount = 0,
            MaxResultCount = 50
        });
        if (assets != null && assets.Items.Count>0)
        {
            foreach (var assetsItem in assets.Items)
            {
                await _assetService.ReleaseAssetAsync(assetsItem.Id, DateTime.UtcNow);
            }
        }
        
        //stop and destroy subscriptions
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscriptions = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscriptions.CurrentVersion != null && !allSubscriptions.CurrentVersion.Version.IsNullOrEmpty())
        {
            var currentVersion = allSubscriptions.CurrentVersion.Version;
            await _blockScanAppService.StopAsync(appId, currentVersion);
            _logger.LogInformation($"[AppDeleteHandler] the CurrentVersion {currentVersion} of App {appId} is stopped.");
        }
        
        if (allSubscriptions.PendingVersion != null && !allSubscriptions.PendingVersion.Version.IsNullOrEmpty())
        {
            var pendingVersion = allSubscriptions.PendingVersion.Version;
            await _blockScanAppService.StopAsync(appId, pendingVersion);
            _logger.LogInformation($"[AppDeleteHandler] the PendingVersion {pendingVersion} of App {appId} is stopped.");
        }
        
        
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(eventData.AppId);
        appInfoIndex.Status = eventData.Status;
        appInfoIndex.DeleteTime = eventData.DeleteTime;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
        
        var organizationIndex = await _organizationEntityMappingRepository.GetAsync(organizationGuid.ToString());
        organizationIndex.AppIds.Remove(eventData.AppId);
        await _organizationEntityMappingRepository.UpdateAsync(organizationIndex);
        
        _logger.LogInformation($"[AppDeleteHandler] App {eventData.AppId} is deleted.");
    }

}