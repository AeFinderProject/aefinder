using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.User;
using AElf.EntityMapping.Elasticsearch.Services;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace AeFinder.Apps;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AppService : AeFinderAppService, IAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IUserAppService _userAppService;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;
    private readonly IElasticIndexService _elasticIndexService;

    public AppService(IClusterClient clusterClient, IUserAppService userAppService,
        IAppResourceLimitProvider appResourceLimitProvider,
        IElasticIndexService elasticIndexService,
        IOrganizationAppService organizationAppService)
    {
        _clusterClient = clusterClient;
        _userAppService = userAppService;
        _organizationAppService = organizationAppService;
        _appResourceLimitProvider = appResourceLimitProvider;
        _elasticIndexService = elasticIndexService;
    }

    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
        dto.AppId = dto.AppName.Trim().ToLower().Replace(" ", "_");
        dto.DeployKey = Guid.NewGuid().ToString("N");
        
        dto.OrganizationId = await GetOrganizationIdAsync();
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(dto.AppId));
        var appDto = await appGrain.CreateAsync(dto);
        await _userAppService.RegisterAppAuthentication(dto.AppId, dto.DeployKey);
        return appDto;
    }

    public async Task<AppDto> UpdateAsync(string appId, UpdateAppDto dto)
    {
        await CheckPermissionAsync(appId);

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        return await appGrain.UpdateAsync(dto);
    }

    public async Task<AppDto> GetAsync(string appId)
    {
        await CheckPermissionAsync(appId);
        
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
        var app = await appGrain.GetAsync();

        var subscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var versions = await subscriptionGrain.GetAllSubscriptionAsync();
        app.Versions.CurrentVersion = versions.CurrentVersion?.Version;
        app.Versions.PendingVersion = versions.PendingVersion?.Version;

        return app;
    }

    public async Task<PagedResultDto<AppDto>> GetListAsync()
    {
        var organizationId = await GetOrganizationIdAsync();
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GenerateOrganizationAppGrainId(organizationId));
        var appIds = await organizationAppGrain.GetAppsAsync();

        var tasks = appIds.Select(async id =>
        {
            var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(id));
            return await appGrain.GetAsync();
        });

        var apps = await tasks.WhenAll();

        return new PagedResultDto<AppDto>
        {
            TotalCount = apps.Length,
            Items = apps.OrderByDescending(o => o.UpdateTime).ToList()
        };
    }

    public async Task<AppSyncStateDto> GetSyncStateAsync(string appId)
    {
        var state = new AppSyncStateDto();
        
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();

        if (allSubscription.CurrentVersion != null)
        {
            var syncStateItems = await GetSyncStateItemsAsync(appId, allSubscription.CurrentVersion);
            state.CurrentVersion = new AppVersionSyncState
            {
                Version = allSubscription.CurrentVersion.Version,
                Items = syncStateItems
            };
        }

        if (allSubscription.PendingVersion != null)
        {
            var syncStateItems = await GetSyncStateItemsAsync(appId, allSubscription.PendingVersion);
            state.PendingVersion = new AppVersionSyncState
            {
                Version = allSubscription.PendingVersion.Version,
                Items = syncStateItems
            };
        }
        
        
        return state;
    }

    public async Task SetMaxAppCountAsync(Guid organizationId, int appCount)
    {
        var orgId = GetOrganizationGrainId(organizationId);
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(orgId);
        await organizationAppGrain.SetMaxAppCountAsync(appCount);
    }

    public async Task<int> GetMaxAppCountAsync(Guid organizationId)
    {
        var orgId = GetOrganizationGrainId(organizationId);
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(orgId);
        return await organizationAppGrain.GetMaxAppCountAsync();
    }

    private async Task<List<AppSyncStateItem>> GetSyncStateItemsAsync(string appId, SubscriptionDetail subscription)
    {
        var appSyncStateItems = new List<AppSyncStateItem>();
        foreach (var subscriptionItem in subscription.SubscriptionManifest.SubscriptionItems)
        {
            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, subscription.Version, subscriptionItem.ChainId));
            var blockStateSetStatus = await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync();
            var appSyncStateItem = new AppSyncStateItem()
            {
                ChainId = subscriptionItem.ChainId,
                LongestChainBlockHash = blockStateSetStatus.LongestChainBlockHash,
                LongestChainHeight = blockStateSetStatus.LongestChainHeight,
                BestChainBlockHash = blockStateSetStatus.BestChainBlockHash,
                BestChainHeight = blockStateSetStatus.BestChainHeight,
                LastIrreversibleBlockHash = blockStateSetStatus.LastIrreversibleBlockHash,
                LastIrreversibleBlockHeight = blockStateSetStatus.LastIrreversibleBlockHeight
            };
            appSyncStateItems.Add(appSyncStateItem);
        }

        return appSyncStateItems;
    }

    private async Task CheckPermissionAsync(string appId)
    {
        var organizationId = await GetOrganizationIdAsync();
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GenerateOrganizationAppGrainId(organizationId));
        var appIds = await organizationAppGrain.GetAppsAsync();
        if (!appIds.Contains(appId))
        {
            throw new UserFriendlyException("No permission!");
        }
    }

    private async Task<string> GetOrganizationIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id.ToString("N");
    }

    private string GetOrganizationGrainId(Guid orgId)
    {
        return GrainIdHelper.GenerateOrganizationAppGrainId(orgId.ToString("N"));
    }
    
    public async Task<string> GetAppCodeAsync(string appId,string version)
    {
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var codeBytes = await appSubscriptionGrain.GetCodeAsync(version);
        return Convert.ToBase64String(codeBytes);
    }

    public async Task<AppResourceLimitDto> SetAppResourceLimitAsync(string appId, SetAppResourceLimitDto dto)
    {
        if (dto == null)
        {
            throw new UserFriendlyException("please input limit parameters");
        }
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));

        await appResourceLimitGrain.SetAsync(dto);

        return await appResourceLimitGrain.GetAsync();
    }

    public async Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId)
    {
        return await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
    }

    public async Task DeleteAppIndexAsync(string indexName)
    {
        await _elasticIndexService.DeleteIndexAsync(indexName);
    }
}