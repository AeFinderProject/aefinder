using AeFinder.Grains.State.Merchandises;
using AeFinder.Merchandises;

namespace AeFinder.Grains.Grain.Merchandises;

public interface IMerchandiseGrain: IGrainWithGuidKey
{
    Task<MerchandiseState> CreateAsync(Guid id, CreateMerchandiseInput input);
    Task<MerchandiseState> UpdateAsync(UpdateMerchandiseInput input);
    Task<MerchandiseState> GetAsync();
}