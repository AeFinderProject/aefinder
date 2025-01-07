using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.AppResources;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppResourceService : AeFinderAppService, IAppResourceService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;
    private readonly IAppDeployManager _appDeployManager;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodIndexRepository;

    public AppResourceService(IClusterClient clusterClient,
        IAppResourceLimitProvider appResourceLimitProvider,
        IAppDeployManager appDeployManager,IDistributedEventBus distributedEventBus,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodIndexRepository)
    {
        _clusterClient = clusterClient;
        _appDeployManager = appDeployManager;
        _distributedEventBus = distributedEventBus;
        _appResourceLimitProvider = appResourceLimitProvider;
        _appSubscriptionPodIndexRepository = appSubscriptionPodIndexRepository;
    }

    public async Task<List<AppResourceDto>> GetAsync(string appId)
    {
        var queryable = await _appSubscriptionPodIndexRepository.GetQueryableAsync();
        var resources = queryable.Where(o => o.AppId == appId).ToList();

        return ObjectMapper.Map<List<AppSubscriptionPodIndex>, List<AppResourceDto>>(resources);
    }
    
    public async Task<AppResourceLimitDto> SetAppResourceLimitAsync(string appId, SetAppResourceLimitDto dto)
    {
        if (dto == null)
        {
            throw new UserFriendlyException("please input limit parameters");
        }
        
        var appOldLimit = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);

        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));

        await appResourceLimitGrain.SetAsync(dto);

        //Publish app limit update eto to background worker
        var appLimit = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        var appLimitUpdateEto = ObjectMapper.Map<AppResourceLimitDto, AppLimitUpdateEto>(appLimit);
        appLimitUpdateEto.AppId = appId;
        await _distributedEventBus.PublishAsync(appLimitUpdateEto);
        
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();
        var currentVersion = allSubscription.CurrentVersion?.Version;
        var pendingVersion = allSubscription.PendingVersion?.Version;

        //Check if need update full pod resource
        if (appOldLimit.AppFullPodRequestCpuCore != appLimit.AppFullPodRequestCpuCore ||
            appOldLimit.AppFullPodRequestMemory != appLimit.AppFullPodRequestMemory ||
            appOldLimit.AppFullPodLimitCpuCore != appLimit.AppFullPodLimitCpuCore ||
            appOldLimit.AppFullPodLimitMemory != appLimit.AppFullPodLimitMemory)
        {
            if (!string.IsNullOrEmpty(currentVersion))
            {
                var chainIds = await GetDeployChainIdAsync(appId, currentVersion);
                await _appDeployManager.UpdateAppFullPodResourceAsync(appId, currentVersion,
                    appLimit.AppFullPodRequestCpuCore, appLimit.AppFullPodRequestMemory, chainIds,
                    appLimit.AppFullPodLimitCpuCore, appLimit.AppFullPodLimitMemory);
            }

            if (!string.IsNullOrEmpty(pendingVersion))
            {
                var chainIds = await GetDeployChainIdAsync(appId, pendingVersion);
                await _appDeployManager.UpdateAppFullPodResourceAsync(appId, pendingVersion,
                    appLimit.AppFullPodRequestCpuCore, appLimit.AppFullPodRequestMemory, chainIds,
                    appLimit.AppFullPodLimitCpuCore, appLimit.AppFullPodLimitMemory);
            }
        }
        //Check if need update query pod resource
        if (appOldLimit.AppQueryPodRequestCpuCore != appLimit.AppQueryPodRequestCpuCore ||
            appOldLimit.AppQueryPodRequestMemory != appLimit.AppQueryPodRequestMemory)
        {
            if (!string.IsNullOrEmpty(currentVersion))
            {
                await _appDeployManager.UpdateAppQueryPodResourceAsync(appId, currentVersion,
                    appLimit.AppQueryPodRequestCpuCore, appLimit.AppQueryPodRequestMemory,null, null, 0);
            }

            if (!string.IsNullOrEmpty(pendingVersion))
            {
                await _appDeployManager.UpdateAppQueryPodResourceAsync(appId, pendingVersion,
                    appLimit.AppQueryPodRequestCpuCore, appLimit.AppQueryPodRequestMemory,null, null, 0);
            }
        }
        
        return await appResourceLimitGrain.GetAsync();
    }
    
    private async Task<List<string>> GetDeployChainIdAsync(string appId, string version)
    {
        var chainIds = new List<string>();
        var enableMultipleInstances = (await _appResourceLimitProvider.GetAppResourceLimitAsync(appId)).EnableMultipleInstances;
        if (enableMultipleInstances)
        {
            chainIds = await GetSubscriptionChainIdAsync(appId, version);
        }

        return chainIds;
    }
    
    private async Task<List<string>> GetSubscriptionChainIdAsync(string appId, string version)
    {
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = await appSubscriptionGrain.GetSubscriptionAsync(version);
        return subscription.SubscriptionItems.Select(o => o.ChainId).ToList();
    }
    
    public async Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId)
    {
        return await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
    }
}