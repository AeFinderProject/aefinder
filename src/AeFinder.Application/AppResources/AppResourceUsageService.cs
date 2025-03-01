using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AeFinder.AppResources.Dto;
using AElf.EntityMapping.Repositories;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using System.Linq;

namespace AeFinder.AppResources;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppResourceUsageService : AeFinderAppService, IAppResourceUsageService
{
    private readonly IEntityMappingRepository<AppResourceUsageIndex, string> _entityMappingRepository;

    public AppResourceUsageService(IEntityMappingRepository<AppResourceUsageIndex, string> entityMappingRepository)
    {
        _entityMappingRepository = entityMappingRepository;
    }

    public async Task AddOrUpdateAsync(List<AppResourceUsageDto> input)
    {
        var index = ObjectMapper.Map<List<AppResourceUsageDto>, List<AppResourceUsageIndex>>(input);
        await _entityMappingRepository.AddOrUpdateManyAsync(index);
    }

    public async Task DeleteAsync(string appId)
    {
        await _entityMappingRepository.DeleteAsync(appId);
    }

    public async Task<PagedResultDto<AppResourceUsageDto>> GetListAsync(Guid? organizationId,
        GetAppResourceUsageInput input)
    {
        var queryable = await _entityMappingRepository.GetQueryableAsync();
        if (organizationId.HasValue)
        {
            queryable = queryable.Where(o => o.OrganizationId == organizationId.Value);
        }

        if (!input.AppId.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(o => o.AppInfo.AppId == input.AppId);
        }

        var count = queryable.Count();
        var list = queryable.OrderBy(o => o.AppInfo.AppId).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<AppResourceUsageDto>
        {
            TotalCount = count,
            Items = ObjectMapper.Map<List<AppResourceUsageIndex>, List<AppResourceUsageDto>>(list)
        };
    }
    
    public async Task<AppResourceUsageDto> GetAsync(Guid? organizationId, string appId)
    {
        var index = await _entityMappingRepository.GetAsync(appId);
        if (index != null && organizationId.HasValue && index.OrganizationId != organizationId.Value)
        {
            throw new UserFriendlyException("No permission.");
        }

        return ObjectMapper.Map<AppResourceUsageIndex, AppResourceUsageDto>(index);
    }
}