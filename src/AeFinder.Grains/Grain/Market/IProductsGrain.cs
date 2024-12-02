using AeFinder.Market;

namespace AeFinder.Grains.Grain.Market;

public interface IProductsGrain: IGrainWithIntegerKey
{
    Task InitializeProductsInfoAsync(ProductDto dto);
    Task<ProductDto> GetProductInfoByIdAsync(string productId);
    Task<int> GetFreeApiQueryCountAsync();
    Task<List<ProductDto>> GetFullPodResourceProductsAsync();
}