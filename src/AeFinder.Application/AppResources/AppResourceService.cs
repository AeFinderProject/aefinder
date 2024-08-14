using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.AppResources;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppResourceService : AeFinderAppService, IAppResourceService
{
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodIndexRepository;

    public AppResourceService(
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodIndexRepository)
    {
        _appSubscriptionPodIndexRepository = appSubscriptionPodIndexRepository;
    }

    public async Task<List<AppResourceDto>> GetAsync(string appId)
    {
        var queryable = await _appSubscriptionPodIndexRepository.GetQueryableAsync();
        var resources = queryable.Where(o => o.AppId == appId).ToList();

        return ObjectMapper.Map<List<AppSubscriptionPodIndex>, List<AppResourceDto>>(resources);
    }
}