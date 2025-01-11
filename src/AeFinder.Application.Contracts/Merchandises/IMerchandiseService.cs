using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Merchandises;

public interface IMerchandiseService
{
    Task AddOrUpdateIndexAsync(MerchandiseChangedEto input);
    Task UpdateIndexAsync(Guid id);
    Task<ListResultDto<MerchandiseDto>> GetListAsync(GetMerchandiseInput input);
    Task<ListResultDto<MerchandiseDto>> GetAllListAsync(GetMerchandiseInput input);
    Task<MerchandiseDto> CreateAsync(CreateMerchandiseInput input);
    Task<MerchandiseDto> UpdateAsync(Guid id, UpdateMerchandiseInput input);
}