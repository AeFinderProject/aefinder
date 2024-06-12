using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.User;
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

    public AppService(IClusterClient clusterClient, IUserAppService userAppService,
        IOrganizationAppService organizationAppService)
    {
        _clusterClient = clusterClient;
        _userAppService = userAppService;
        _organizationAppService = organizationAppService;
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

    public async Task<AppSyncStateDto> GetSyncStateAsync(string appId, string version = null)
    {
        var state = new AppSyncStateDto();
        
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(appId));

        var subscriptionManifest = new SubscriptionManifest();
        if (version.IsNullOrWhiteSpace())
        {
            // TODO: Get current version from cache
            var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();
            if (allSubscription.CurrentVersion != null)
            {
                version = allSubscription.CurrentVersion.Version;
                subscriptionManifest = allSubscription.CurrentVersion.SubscriptionManifest;
            }
        }
        else
        {
            subscriptionManifest = await appSubscriptionGrain.GetSubscriptionAsync(version);
        }

        foreach (var subscriptionItem in subscriptionManifest.SubscriptionItems)
        {
            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, subscriptionItem.ChainId));
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
            state.Items.Add(appSyncStateItem);
        }
        
        return state;
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
}