using System;
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
    
    public async Task<List<FullPodResourceDto>> GetFullPodResourceInfoAsync()
    {
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var fullPodResourceProducts = await productsGrain.GetFullPodResourceProductsAsync();
        var resultList = new List<FullPodResourceDto>();
        foreach (var productDto in fullPodResourceProducts)
        {
            var resourceDto = ConvertToFullPodResourceDto(productDto);
            resultList.Add(resourceDto);
        }

        return resultList;
    }

    public FullPodResourceDto ConvertToFullPodResourceDto(ProductDto productDto)
    {
        var podResourceDto = new FullPodResourceDto();
        podResourceDto.ProductId = productDto.ProductId;
        podResourceDto.LevelName = productDto.ProductSpecifications;
        var resourceCapacity = JsonConvert.DeserializeObject<ResourceCapacity>(productDto.Description);
        podResourceDto.Capacity = resourceCapacity;
        podResourceDto.MonthlyUnitPrice = productDto.MonthlyUnitPrice;
        return podResourceDto;
    }

    public async Task<ApiQueryCountResourceDto> GetRegularApiQueryCountProductInfoAsync()
    {
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetRegularApiQueryCountProductAsync();
        var resourceDto = new ApiQueryCountResourceDto();
        resourceDto.ProductId = productInfo.ProductId;
        resourceDto.QueryCount = Convert.ToInt32(productInfo.ProductSpecifications);
        resourceDto.MonthlyUnitPrice = productInfo.MonthlyUnitPrice;
        return resourceDto;
    }
}