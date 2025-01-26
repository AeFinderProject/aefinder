using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.Merchandises;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using System.Linq;
using AeFinder.Grains.State.Merchandises;

namespace AeFinder.Merchandises;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class MerchandiseService : AeFinderAppService, IMerchandiseService
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<MerchandiseIndex, Guid> _merchandiseIndexRepository;

    public MerchandiseService(IClusterClient clusterClient,
        IEntityMappingRepository<MerchandiseIndex, Guid> merchandiseIndexRepository)
    {
        _clusterClient = clusterClient;
        _merchandiseIndexRepository = merchandiseIndexRepository;
    }
    
    public async Task AddOrUpdateIndexAsync(MerchandiseChangedEto input)
    {
        var index = ObjectMapper.Map<MerchandiseChangedEto, MerchandiseIndex>(input);
        await _merchandiseIndexRepository.AddOrUpdateAsync(index);
    }
    
    public async Task UpdateIndexAsync(Guid id)
    {
        var grain = _clusterClient.GetGrain<IMerchandiseGrain>(id);
        var merchandise = await grain.GetAsync();
        var eto = ObjectMapper.Map<MerchandiseState, MerchandiseChangedEto>(merchandise);
        await AddOrUpdateIndexAsync(eto);
    }

    public async Task<ListResultDto<MerchandiseDto>> GetListAsync(GetMerchandiseInput input)
    {
        var queryable = await _merchandiseIndexRepository.GetQueryableAsync();
        queryable = queryable.Where(o => o.Status == (int)MerchandiseStatus.Listed);
        if (input.Type.HasValue)
        {
            queryable = queryable.Where(o => o.Type == (int)input.Type.Value);
        }
        
        if (input.Category.HasValue)
        {
            queryable = queryable.Where(o => o.Category == (int)input.Category.Value);
        }
        
        var indices = queryable.OrderBy(o => o.SortWeight).ToList();

        return new ListResultDto<MerchandiseDto>
        {
            Items = ObjectMapper.Map<List<MerchandiseIndex>, List<MerchandiseDto>>(indices)
        };
    }
    
    public async Task<ListResultDto<MerchandiseDto>> GetAllListAsync(GetMerchandiseInput input)
    {
        var queryable = await _merchandiseIndexRepository.GetQueryableAsync();
        if (input.Type.HasValue)
        {
            queryable = queryable.Where(o => o.Type == (int)input.Type.Value);
        }
        
        if (input.Category.HasValue)
        {
            queryable = queryable.Where(o => o.Category == (int)input.Category.Value);
        }
        
        var indices = queryable.OrderBy(o => o.SortWeight).ToList();

        return new ListResultDto<MerchandiseDto>
        {
            Items = ObjectMapper.Map<List<MerchandiseIndex>, List<MerchandiseDto>>(indices)
        };
    }

    public async Task<MerchandiseDto> CreateAsync(CreateMerchandiseInput input)
    {
        var id = GuidGenerator.Create();
        var grain = _clusterClient.GetGrain<IMerchandiseGrain>(id);
        var merchandise = await grain.CreateAsync(id, input);
        return ObjectMapper.Map<MerchandiseState, MerchandiseDto>(merchandise);
    }

    public async Task<MerchandiseDto> UpdateAsync(Guid id, UpdateMerchandiseInput input)
    {
        var grain = _clusterClient.GetGrain<IMerchandiseGrain>(id);
        var merchandise =  await grain.UpdateAsync(input);
        return ObjectMapper.Map<MerchandiseState, MerchandiseDto>(merchandise);
    }
}