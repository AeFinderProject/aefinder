using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.Apps.Dto;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.AppResources;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppResourceService : AeFinderAppService, IAppResourceService
{
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodIndexRepository;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;

    public AppResourceService(
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodIndexRepository,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository)
    {
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
        // var totalCount = queryable.Count();
        var podsResourceInfoList = ObjectMapper.Map<List<AppPodInfoIndex>, List<AppPodInfoDto>>(pods);
        var fullPodResourUsageInfoList = new List<AppFullPodResourceUsageDto>();
        foreach (var podResourceInfo in podsResourceInfoList)
        {
            foreach (var containerDto in podResourceInfo.Containers)
            {
                if (containerDto.ContainerName.Contains("-full"))
                {
                    var fullPodResourceUsage = ObjectMapper.Map<PodContainerDto, AppFullPodResourceUsageDto>(containerDto);
                    fullPodResourceUsage.AppId = podResourceInfo.AppId;
                    fullPodResourceUsage.AppVersion = podResourceInfo.AppVersion;
                    fullPodResourUsageInfoList.Add(fullPodResourceUsage);
                }
            }
        }
        return fullPodResourUsageInfoList;
    }
}