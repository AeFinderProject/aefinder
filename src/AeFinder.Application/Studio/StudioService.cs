using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
    private readonly IKubernetesAppManager _kubernetesAppManager;
    private readonly StudioOption _studioOption;
    private readonly ICodeAuditor _codeAuditor;
    private readonly ILogger<StudioService> _logger;
    private readonly IObjectMapper _objectMapper;

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper,
        IBlockScanAppService blockScanAppService, ICodeAuditor codeAuditor, IOptions<StudioOption> studioOption, IKubernetesAppManager kubernetesAppManager)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanAppService = blockScanAppService;
        _codeAuditor = codeAuditor;
        _studioOption = studioOption.Value;
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput appNameInput)
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

    public async Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
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
        await AddToUsersApps(userIds, appInfo.AppId, _objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        return new AddOrUpdateAeFinderAppDto();
    }

    public async Task<AeFinderAppInfoDto> GetAeFinderApp()
    {
        var userId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request GetAeFinderApp: adminId= {0}", userId);
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        return info == null ? null : _objectMapper.Map<AeFinderAppInfo, AeFinderAppInfoDto>(info);
    }

    //to be finished
    private async void AuthDeveloper(string appId, string clientId = null)
    {
        if (!clientId.IsNullOrEmpty() && clientId.Equals(appId))
        {
            return;
        }

        var userId = CurrentUser.GetId().ToString("N");
        if (!IsAdminOf(userId, appId))
        {
            var nameGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(appId));
            if (!await nameGrain.IsDeveloper(userId))
            {
                _logger.LogError("GetAeFinderApp: {0} auth failed {1}", CurrentUser.GetId(), appId);
                throw new UserFriendlyException("You are not developer of this app.");
            }
        }
    }

    //to be finished
    public async Task<AddDeveloperToAppDto> AddDeveloperToApp(AddDeveloperToAppInput input)
    {
        var adminId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request UpdateAeFinderApp: adminId= {0} input= {1}", adminId, JsonSerializer.Serialize(input));
        if (!IsAdminOf(adminId, input.AppId))
        {
            _logger.LogError("UpdateAeFinderApp: {0} is not admin of {1}", CurrentUser.GetId(), input.AppId);
            throw new UserFriendlyException("You are not admin of this app.");
        }

        var nameGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(input.AppId));
        var appInfo = await nameGrain.AddDeveloperToApp(input.DeveloperId);
        if (appInfo != null)
        {
            await _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(input.DeveloperId)).AddApp(input.AppId, appInfo.AeFinderAppInfo);
        }

        return new AddDeveloperToAppDto();
    }

    public async Task<List<AeFinderAppInfo>> GetAeFinderAppList()
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

    public async Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input)
    {
        SubscriptionManifestDto subscriptionManifestDto;

        try
        {
            subscriptionManifestDto = JsonSerializer.Deserialize<SubscriptionManifestDto>(input.SubscriptionManifest);
        }
        catch (Exception)
        {
            throw new UserFriendlyException("Invalid subscription manifest.");
        }

        if (subscriptionManifestDto == null || subscriptionManifestDto.SubscriptionItems.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid subscription manifest.");
        }

        await using var stream = input.AppDll.OpenReadStream();
        var dllBytes = stream.GetAllBytes();
        _codeAuditor.Audit(dllBytes);
        var userId = CurrentUser.GetId().ToString("N");
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        if (info == null || info.AppId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("app not exists.");
        }

        var version = await _blockScanAppService.AddSubscriptionAsync(info.AppId, subscriptionManifestDto, dllBytes);
        var rulePath = await _kubernetesAppManager.CreateNewAppPodAsync(info.AppId, version, _studioOption.ImageName);
        _logger.LogInformation("SubmitSubscriptionInfoAsync: {0} {1} {2}", info.AppId, version, rulePath);
        // var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(input.AppId));
        // await appGrain.SetGraphQlByVersion(version, appGraphQl);
        return version;
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

        await _kubernetesAppManager.DestroyAppPodAsync(info.AppId, version);
        _logger.LogInformation("DestroyAppAsync: {0} {1}", info.AppId, version);
    }

    public async Task<QueryAeFinderAppDto> QueryAeFinderAppAsync(QueryAeFinderAppInput input)
    {
        var userId = CurrentUser.GetId().ToString("N");
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var ans = await appGrain.GetGraphQls();
        return new QueryAeFinderAppDto() { GraphQLs = ans };
    }

    public Task<QueryAeFinderAppLogsDto> QueryAeFinderAppLogsAsync(QueryAeFinderAppLogsInput input)
    {
        throw new NotImplementedException();
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
        _codeAuditor.Audit(dllBytes);

        var appId = await GetAppIdAsync();
        var subscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await subscriptionGrain.UpdateCodeAsync(input.Version, dllBytes);
        return new UpdateAeFinderAppDto() { Success = true };
    }

    public async Task RestartAppAsync(string version)
    {
        var appId = await GetAppIdAsync();
        await _kubernetesAppManager.RestartAppPodAsync(appId, version);

    }

    private async Task AddToUsersApps(IEnumerable<string> userIds, string appId, AeFinderAppInfo info)
    {
        var tasks = userIds.Select(userId => _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId))).Select(userAppsGrain => userAppsGrain.AddApp(appId, info)).ToList();
        await tasks.WhenAll();
    }

    private bool IsAdminOf(string adminId, string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(adminId));
        return appGrain.IsAdmin(appId).Result;
    }
}