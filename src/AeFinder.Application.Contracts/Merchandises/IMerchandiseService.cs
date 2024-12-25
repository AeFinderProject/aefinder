using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AeFinder.Merchandises;

public interface IMerchandiseService
{
    Task AddOrUpdateIndexAsync(MerchandiseChangedEto input);
    Task<ListResultDto<MerchandiseDto>> GetList(GetMerchandiseInput input);
    Task<ListResultDto<MerchandiseDto>> GetAllList(GetMerchandiseInput input);
    Task<MerchandiseDto> CreateAsync(CreateMerchandiseInput input);
    Task<MerchandiseDto> UpdateAsync(Guid id, UpdateMerchandiseInput input);
}