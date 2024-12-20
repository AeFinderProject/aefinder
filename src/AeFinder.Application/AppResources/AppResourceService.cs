using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.Apps.Dto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.AppResources;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppResourceService : AeFinderAppService, IAppResourceService
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodIndexRepository;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;

    public AppResourceService(IOrganizationAppService organizationAppService, IClusterClient clusterClient,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodIndexRepository,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _appSubscriptionPodIndexRepository = appSubscriptionPodIndexRepository;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
    }

    public async Task<List<AppResourceDto>> GetAsync(string appId)
    {
        var queryable = await _appSubscriptionPodIndexRepository.GetQueryableAsync();
        var resources = queryable.Where(o => o.AppId == appId).ToList();

        return ObjectMapper.Map<List<AppSubscriptionPodIndex>, List<AppResourceDto>>(resources);
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
                GrainIdHelper.GetOrganizationGrainIdAsync(organizationId));
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