using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.User;
using Microsoft.Extensions.Logging;
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
    private readonly IOrganizationAppService _organizationAppService;

    public RenewalService(IClusterClient clusterClient,IProductService productService,IOrganizationAppService organizationAppService)
    {
        _clusterClient = clusterClient;
        _productService = productService;
        _organizationAppService = organizationAppService;
    }
    
    public async Task<int> GetUserApiQueryFreeCountAsync()
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();

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
    
    public async Task<int> GetUserApiQueryFreeCountAsync(string organizationId)
    {
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

    public async Task<long> GetUserMonthlyApiQueryAllowanceAsync()
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var queryCount =
            await renewalGrain.GetOrganizationMonthlyApiQueryAllowanceAsync(organizationId);
        return queryCount;
    }
    
    public async Task<long> GetUserMonthlyApiQueryAllowanceAsync(string organizationId)
    {
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var queryCount =
            await renewalGrain.GetOrganizationMonthlyApiQueryAllowanceAsync(organizationId);
        return queryCount;
    }

    public async Task<FullPodResourceDto> GetUserCurrentFullPodResourceAsync(string appId)
    {
        Logger.LogInformation("[GetUserCurrentFullPodResourceAsync]CurrentUser.Id:" + CurrentUser.Id.ToString());
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var renewalInfo =
            await renewalGrain.GetCurrentPodResourceRenewalInfoAsync(organizationId, appId);
        if (renewalInfo == null)
        {
            return new FullPodResourceDto();
        }
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(renewalInfo.ProductId);
        var resourceDto = _productService.ConvertToFullPodResourceDto(productInfo);
        return resourceDto;
    }

    public async Task<List<RenewalDto>> GetAllActiveRenewalListAsync()
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var renewalGrain =
            _clusterClient.GetGrain<IRenewalGrain>(
                GrainIdHelper.GenerateRenewalGrainId(Guid.Parse(organizationId)));
        var renewalList = await renewalGrain.GetAllActiveRenewalInfosAsync(organizationId);
        return renewalList;
    }
}