using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Kubernetes.Manager;
using AeFinder.Option;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace AeFinder.Studio;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class StudioService : AeFinderAppService, IStudioService, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly StudioOption _studioOption;
    private readonly ICodeAuditor _codeAuditor;
    private readonly ILogger<StudioService> _logger;
    private readonly IObjectMapper _objectMapper;

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper,
        IBlockScanAppService blockScanAppService, ICodeAuditor codeAuditor, IOptionsSnapshot<StudioOption> studioOption, IAppDeployManager kubernetesAppManager)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanAppService = blockScanAppService;
        _codeAuditor = codeAuditor;
        _studioOption = studioOption.Value;
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppNameAsync(ApplyAeFinderAppNameInput appNameInput)
    {
        var userId = CurrentUser.GetId().ToString("N");

        _logger.LogInformation("receive request ApplyAeFinderAppName: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(appNameInput));

        // app name must be unique
        var appNameGrain = _clusterClient.GetGrain<IAppNameGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(appNameInput.Name));
        var appId = await appNameGrain.Register(userId);
        if (appId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("App name already exists.");
        }

        //appid must not be registered
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var res = await appGrain.Register(userId, appNameInput.Name, appNameInput.Name);

        var ans = new ApplyAeFinderAppNameDto() { Success = res.Success, AppId = appNameInput.Name };
        _logger.LogInformation("response ApplyAeFinderAppName: {0} input={1} exists={2} added={3}", userId, JsonSerializer.Serialize(appNameInput), res.Success, res.Added);
        return ans;
    }

    public async Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderAppAsync(AddOrUpdateAeFinderAppInput input)
    {
        var userId = CurrentUser.GetId().ToString("N");

        _logger.LogInformation("receive request UpdateAeFinderApp: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(input));

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var appInfo = await appGrain.AddOrUpdateAppInfo(_objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        if (appInfo == null)
        {
            throw new UserFriendlyException("App not exists.");
        }

        var userIds = new List<string>() { userId };
        if (appInfo.DeveloperIds != null)
        {
            userIds.AddRange(appInfo.DeveloperIds);
        }


        // we do not wait for the result of AddToUsersApps
        await AddToUsersAppsAsync(userIds, appInfo.AppId, _objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        return new AddOrUpdateAeFinderAppDto();
    }

    public async Task<AeFinderAppInfoDto> GetAeFinderAppAsync()
    {
        var userId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request GetAeFinderApp: adminId= {0}", userId);
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        return info == null ? null : _objectMapper.Map<AeFinderAppInfo, AeFinderAppInfoDto>(info);
    }

    public async Task<List<AeFinderAppInfo>> GetAeFinderAppListAsync()
    {
        var userId = CurrentUser.GetId().ToString("N");
        var apps = await _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId)).GetApps();
        var response = new List<AeFinderAppInfo>();
        if (!apps.IsNullOrEmpty())
        {
            response.AddRange(apps.Select(app => new AeFinderAppInfo()
            {
                AppId = app.Key,
                DisplayName = app.Value.DisplayName,
                Description = app.Value.Description,
                LogoUrl = app.Value.LogoUrl,
                SourceCodeUrl = app.Value.SourceCodeUrl
            }));
        }

        return response;
    }

    public async Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input, SubscriptionManifestDto subscriptionManifest)
    {
        if (subscriptionManifest == null || subscriptionManifest.SubscriptionItems.Count == 0)
        {
            throw new UserFriendlyException("invalid subscription manifest.");
        }

        if (subscriptionManifest.SubscriptionItems.Any(item => item.ChainId.IsNullOrEmpty() || item.StartBlockNumber < 0))
        {
            throw new UserFriendlyException("invalid subscription manifest.");
        }

        await using var stream = input.AppDll.OpenReadStream();
        var dllBytes = stream.GetAllBytes();
        try
        {
            _codeAuditor.Audit(dllBytes);
        }
        catch (Exception e)
        {
            // throw new UserFriendlyException("audit failed: " + e.Message);
        }

        var userId = CurrentUser.GetId().ToString("N");
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        if (info == null || info.AppId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("app not exists.");
        }

        var dto = await _blockScanAppService.AddSubscriptionV2Async(info.AppId, subscriptionManifest, dllBytes);
        if (!dto.StopVersion.IsNullOrEmpty())
        {
            await _kubernetesAppManager.DestroyAppAsync(info.AppId, dto.StopVersion);
        }

        var rulePath = await _kubernetesAppManager.CreateNewAppAsync(info.AppId, dto.NewVersion, _studioOption.ImageName);
        _logger.LogInformation("SubmitSubscriptionInfoAsync: {0} {1} {2} stoped version {3}", info.AppId, dto.NewVersion, rulePath, dto.StopVersion);
        return dto.NewVersion;
    }

    public async Task DestroyAppAsync(string version)
    {
        var userId = CurrentUser.GetId().ToString("N");
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        if (info == null || info.AppId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("app not exists.");
        }

        await AssertAppVersionExistsAsync(info.AppId, version);

        await _kubernetesAppManager.DestroyAppAsync(info.AppId, version);
        _logger.LogInformation("DestroyAppAsync: {0} {1}", info.AppId, version);
    }

    private async Task AssertAppVersionExistsAsync(string appId, string version)
    {
        var subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        if (subscription == null || (!subscription.NewVersion.Version.Equals(version) && !subscription.CurrentVersion.Version.Equals(version)))
        {
            throw new UserFriendlyException($"subscription not exists. appId={1} version={2}", appId, version);
        }
    }

    public async Task<string> GetAppIdAsync()
    {
        if (CurrentUser?.Id == null)
        {
            throw new UserFriendlyException("userid not found");
        }

        var userId = CurrentUser.GetId().ToString("N");
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await appGrain.GetAppInfo();
        if (info != null && !info.AppId.IsNullOrEmpty())
        {
            return info.AppId;
        }

        throw new UserFriendlyException("app of current user not found");
    }

    public async Task<UpdateAeFinderAppDto> UpdateAeFinderAppAsync(UpdateAeFinderAppInput input)
    {
        await using var stream = input.AppDll.OpenReadStream();
        var dllBytes = stream.GetAllBytes();
        try
        {
            _codeAuditor.Audit(dllBytes);
        }
        catch (Exception e)
        {
            // throw new UserFriendlyException("audit failed: " + e.Message);
        }

        var appId = await GetAppIdAsync();
        await AssertAppVersionExistsAsync(appId, input.Version);

        var subscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await subscriptionGrain.UpdateCodeAsync(input.Version, dllBytes);
        await _kubernetesAppManager.RestartAppAsync(appId, input.Version);
        return new UpdateAeFinderAppDto() { Success = true };
    }

    public async Task RestartAppAsync(string version)
    {
        var appId = await GetAppIdAsync();
        await AssertAppVersionExistsAsync(appId, version);

        await _kubernetesAppManager.RestartAppAsync(appId, version);
    }

    private async Task AddToUsersAppsAsync(IEnumerable<string> userIds, string appId, AeFinderAppInfo info)
    {
        var tasks = userIds.Select(userId => _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId))).Select(userAppsGrain => userAppsGrain.AddApp(appId, info)).ToList();
        await tasks.WhenAll();
    }
}