using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.Market;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ProductService : ApplicationService, IProductService
{
    private readonly IClusterClient _clusterClient;

    public ProductService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public async Task<List<FullPodResourceLevelDto>> GetFullPodResourceLevelInfoAsync()
    {
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var fullPodResourceProducts = await productsGrain.GetFullPodResourceProductsAsync();
        var resultList = new List<FullPodResourceLevelDto>();
        foreach (var productDto in fullPodResourceProducts)
        {
            var levelDto = new FullPodResourceLevelDto();
            levelDto.ProductId = productDto.ProductId;
            levelDto.LevelName = productDto.ProductSpecifications;
            var resourceCapacity = JsonConvert.DeserializeObject<ResourceCapacity>(productDto.Description);
            levelDto.Capacity = resourceCapacity;
            levelDto.MonthlyUnitPrice = productDto.MonthlyUnitPrice;
            resultList.Add(levelDto);
        }

        return resultList;
    }
}