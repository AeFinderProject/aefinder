using AeFinder.Merchandises;

namespace AeFinder.Grains.Grain.Merchandises;

public interface IMerchandiseGrain: IGrainWithGuidKey
{
    Task<MerchandiseDto> CreateAsync(Guid id, CreateMerchandiseInput input);
    Task<MerchandiseDto> UpdateAsync(UpdateMerchandiseInput input);
    Task<MerchandiseDto> GetAsync();
}