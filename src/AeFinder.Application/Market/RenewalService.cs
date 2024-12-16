using System;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.Market;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class RenewalService: ApplicationService, IRenewalService
{
    private readonly IClusterClient _clusterClient;
    private readonly IProductService _productService;

    public RenewalService(IClusterClient clusterClient,IProductService productService)
    {
        _clusterClient = clusterClient;
        _productService = productService;
    }
    
    public async Task<int> GetUserApiQueryFreeCountAsync(string organizationId)
    {
        //TODO: Check organization id

        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var freeProduct = await productsGrain.GetFreeApiQueryCountProductAsync();
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        if (await renewalGrain.CheckRenewalInfoIsExistAsync(organizationId, freeProduct.ProductId))
        {
            return Convert.ToInt32(freeProduct.ProductSpecifications);
        }

        return 0;
    }

    public async Task<int> GetUserMonthlyApiQueryAllowanceAsync(string organizationId)
    {
        //TODO: Check organization id
        
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var queryCount =
            await renewalGrain.GetOrganizationMonthlyApiQueryAllowanceAsync(organizationId);
        return queryCount;
    }

    public async Task<FullPodResourceDto> GetUserCurrentFullPodResourceAsync(string organizationId,string appId)
    {
        //TODO: Check organization id
        
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var renewalInfo =
            await renewalGrain.GetCurrentPodResourceRenewalInfoAsync(organizationId, appId);
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        var resourceDto = _productService.ConvertToFullPodResourceDto(productInfo);
        return resourceDto;
    }
}