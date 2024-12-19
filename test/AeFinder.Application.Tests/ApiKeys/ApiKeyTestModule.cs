using System;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Core.Clusters;
using Orleans;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.ApiKeys;

[DependsOn(typeof(AeFinderApplicationTestModule))]
public class ApiKeyTestModule : AbpModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var cluster = context.ServiceProvider.GetRequiredService<IClusterClient>();
        
        var productsGrain =
            cluster.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        
        var productDto = new ProductDto();
        productDto.ProductId = Guid.NewGuid().ToString();
        productDto.ProductSpecifications = "100000";
        productDto.ProductType = ProductType.ApiQueryCount;
        productDto.ProductName = "ApiQuery";
        productDto.Description = "ApiQuery";
        productDto.MonthlyUnitPrice = 4;
        productDto.IsActive = true;
        AsyncHelper.RunSync(async () => await productsGrain.InitializeProductsInfoAsync(productDto));
    }
}