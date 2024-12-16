using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IProductService
{
    Task<List<FullPodResourceDto>> GetFullPodResourceInfoAsync();
    FullPodResourceDto ConvertToFullPodResourceDto(ProductDto productDto);
    Task<ApiQueryCountResourceDto> GetRegularApiQueryCountProductInfoAsync();
}