using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.AppResources.Dto;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Options;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Options;
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
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    private readonly AppDeployOptions _appDeployOptions;

    public AppResourceService(IClusterClient clusterClient,
        IAppResourceLimitProvider appResourceLimitProvider,
        IAppDeployManager appDeployManager,IDistributedEventBus distributedEventBus,
        IOrganizationAppService organizationAppService,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodIndexRepository, 
        IOptionsSnapshot<AppDeployOptions> appDeployOptions)
    {
        _clusterClient = clusterClient;
        _appDeployManager = appDeployManager;
        _distributedEventBus = distributedEventBus;
        _appResourceLimitProvider = appResourceLimitProvider;
        _appSubscriptionPodIndexRepository = appSubscriptionPodIndexRepository;
        _appDeployOptions = appDeployOptions.Value;
        _organizationAppService = organizationAppService;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
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

        var updatedCurrentVersion = true;
        var updatedPendingVersion = true;

        //Check if need update full pod resource
        if (appOldLimit.AppFullPodRequestCpuCore != appLimit.AppFullPodRequestCpuCore ||
            appOldLimit.AppFullPodRequestMemory != appLimit.AppFullPodRequestMemory ||
            appOldLimit.AppFullPodLimitCpuCore != appLimit.AppFullPodLimitCpuCore ||
            appOldLimit.AppFullPodLimitMemory != appLimit.AppFullPodLimitMemory)
        {
            if (!string.IsNullOrEmpty(currentVersion))
            {
                var chainIds = await GetDeployChainIdAsync(appId, currentVersion);
                if (!await _appDeployManager.UpdateAppFullPodResourceAsync(appId, currentVersion,
                        appLimit.AppFullPodRequestCpuCore, appLimit.AppFullPodRequestMemory, chainIds,
                        appLimit.AppFullPodLimitCpuCore, appLimit.AppFullPodLimitMemory))
                {
                    updatedCurrentVersion = false;
                }
            }

            if (!string.IsNullOrEmpty(pendingVersion))
            {
                var chainIds = await GetDeployChainIdAsync(appId, pendingVersion);
                if (!await _appDeployManager.UpdateAppFullPodResourceAsync(appId, pendingVersion,
                        appLimit.AppFullPodRequestCpuCore, appLimit.AppFullPodRequestMemory, chainIds,
                        appLimit.AppFullPodLimitCpuCore, appLimit.AppFullPodLimitMemory))
                {
                    updatedPendingVersion = false;
                }
            }
        }
        //Check if need update query pod resource
        if (appOldLimit.AppQueryPodRequestCpuCore != appLimit.AppQueryPodRequestCpuCore ||
            appOldLimit.AppQueryPodRequestMemory != appLimit.AppQueryPodRequestMemory)
        {
            if (!string.IsNullOrEmpty(currentVersion))
            {
                if (!await _appDeployManager.UpdateAppQueryPodResourceAsync(appId, currentVersion,
                        appLimit.AppQueryPodRequestCpuCore, appLimit.AppQueryPodRequestMemory, null, null, 0))
                {
                    updatedCurrentVersion = false;
                }
            }

            if (!string.IsNullOrEmpty(pendingVersion))
            {
                if (!await _appDeployManager.UpdateAppQueryPodResourceAsync(appId, pendingVersion,
                        appLimit.AppQueryPodRequestCpuCore, appLimit.AppQueryPodRequestMemory, null, null, 0))
                {
                    updatedPendingVersion = false; 
                }
            }
        }

        if (!updatedCurrentVersion)
        {
            await DeployNewAppAsync(appId, currentVersion);
        }

        if (!updatedPendingVersion)
        {
            await DeployNewAppAsync(appId, pendingVersion);
        }

        return await appResourceLimitGrain.GetAsync();
    }

    private async Task DeployNewAppAsync(string appId, string version)
    {
        var chainIds = await GetDeployChainIdAsync(appId, version);
        await _appDeployManager.CreateNewAppAsync(appId, version, _appDeployOptions.AppImageName, chainIds);
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
    
    public async Task<List<AppFullPodResourceUsageDto>> GetAppFullPodResourceUsageInfoListAsync(
        GetAppFullPodResourceInfoInput input)
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        //Check appid is belong to user's organization
        var organizationAppGain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GetOrganizationGrainId(organizationId));
        var appIds = await organizationAppGain.GetAppsAsync();
        if (!appIds.Contains(input.AppId))
        {
            throw new UserFriendlyException("app id is invalid.");
        }
        
        var queryable = await _appPodInfoEntityMappingRepository.GetQueryableAsync();
        if (!string.IsNullOrEmpty(input.AppId))
        {
            queryable = queryable.Where(o => o.AppId == input.AppId);
        }

        if (!string.IsNullOrEmpty(input.Version))
        {
            queryable = queryable.Where(o => o.AppVersion == input.Version);
        }

        var pods = queryable.OrderBy(o => o.PodName).ToList();
        var podsResourceInfoList = ObjectMapper.Map<List<AppPodInfoIndex>, List<AppPodInfoDto>>(pods);
        var fullPodResourceUsageInfoList = new List<AppFullPodResourceUsageDto>();
        foreach (var podResourceInfo in podsResourceInfoList)
        {
            foreach (var containerDto in podResourceInfo.Containers)
            {
                if (containerDto.ContainerName.Contains("-full"))
                {
                    var fullPodResourceUsage = ObjectMapper.Map<PodContainerDto, AppFullPodResourceUsageDto>(containerDto);
                    fullPodResourceUsage.AppId = podResourceInfo.AppId;
                    fullPodResourceUsage.AppVersion = podResourceInfo.AppVersion;
                    fullPodResourceUsageInfoList.Add(fullPodResourceUsage);
                }
            }
        }
        return fullPodResourceUsageInfoList;
    }
}