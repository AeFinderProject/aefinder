using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Merchandises;
using AElf.EntityMapping.Repositories;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using System.Linq;
using AeFinder.ApiKeys;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.State.Assets;
using AeFinder.Orders;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Timing;

namespace AeFinder.Assets;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AssetInitializationService : AeFinderAppService, IAssetInitializationService
{
    private readonly IAssetService _assetService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IMerchandiseService _merchandiseService;
    private readonly IClock _clock;
    private readonly AssetInitializationOptions _assetInitializationOptions;
    private readonly IEntityMappingRepository<MerchandiseIndex, Guid> _merchandiseIndexRepository;

    public AssetInitializationService(IMerchandiseService merchandiseService, IAssetService assetService,
        IApiKeyService apiKeyService, IClock clock,
        IOptionsSnapshot<AssetInitializationOptions> assetInitializationOptions,
        IEntityMappingRepository<MerchandiseIndex, Guid> merchandiseIndexRepository)
    {
        _merchandiseService = merchandiseService;
        _assetService = assetService;
        _apiKeyService = apiKeyService;
        _clock = clock;
        _merchandiseIndexRepository = merchandiseIndexRepository;
        _assetInitializationOptions = assetInitializationOptions.Value;
    }

    public async Task InitializeAsync(Guid organizationId)
    {
        var time = _clock.Now;
        foreach (var item in _assetInitializationOptions.Assets)
        {
            var merchandise = await _merchandiseIndexRepository.GetAsync(Guid.Parse(item.MerchandiseId));
            
            if (merchandise.Type == (int)MerchandiseType.ApiQuery)
            {
                var existAssets = await _assetService.GetListAsync(organizationId, new GetAssetInput
                {
                    Type = MerchandiseType.ApiQuery
                });

                if (existAssets.Items.Count > 0)
                {
                    continue;
                }
            }
            
            var asset = await _assetService.CreateAsync(organizationId, new CreateAssetInput
            {
                MerchandiseId = merchandise.Id,
                Quantity = item.Quantity,
                Replicas = item.Replicas,
                FreeQuantity = item.FreeQuantity,
                FreeReplicas = item.FreeReplicas,
                FreeType = item.AssetFreeType,
                CreateTime = time
            });

            if (merchandise.Type == (int)MerchandiseType.ApiQuery)
            {
                await _assetService.StartUsingAssetAsync(asset.Id, time);
                await _apiKeyService.SetQueryLimitAsync(organizationId, item.Quantity);
            }
        }
    }
}