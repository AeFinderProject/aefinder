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
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var freeProduct = await productsGrain.GetFreeApiQueryCountProductAsync();
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        if (await renewalGrain.CheckRenewalInfoIsExistAsync(organizationId, CurrentUser.Id.ToString(),
                freeProduct.ProductId))
        {
            return Convert.ToInt32(freeProduct.ProductSpecifications);
        }

        return 0;
    }

    public async Task<int> GetUserMonthlyApiQueryAllowanceAsync(string organizationId)
    {
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var queryCount =
            await renewalGrain.GetUserMonthlyApiQueryAllowanceAsync(organizationId, CurrentUser.Id.ToString());
        return queryCount;
    }

    public async Task<FullPodResourceLevelDto> GetUserCurrentFullPodResourceAsync(string organizationId,string appId)
    {
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var renewalInfo =
            await renewalGrain.GetCurrentPodResourceRenewalInfoAsync(organizationId, CurrentUser.Id.ToString(), appId);
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        var levelDto = _productService.ConvertToPodResourceLevelDto(productInfo);
        return levelDto;
    }
}