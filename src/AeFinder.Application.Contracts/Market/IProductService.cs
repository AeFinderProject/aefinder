using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IProductService
{
    Task<List<FullPodResourceLevelDto>> GetFullPodResourceLevelInfoAsync();
    FullPodResourceLevelDto ConvertToPodResourceLevelDto(ProductDto productDto);
}