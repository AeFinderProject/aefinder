using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
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
    private readonly ICodeAuditor _codeAuditor;
    private readonly IKubernetesAppManager _kubernetesAppManager;
    private readonly StudioOption _studioOption;
    private readonly ILogger<StudioService> _logger;
    private readonly IObjectMapper _objectMapper;

    public async Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(string appId, ApplyAeFinderAppNameInput appNameInput)
    {
        var userId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request ApplyAeFinderAppName: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(appNameInput));

        // app name must be unique
        var appNameGrain = _clusterClient.GetGrain<IAppNameGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(appNameInput.Name));
        var success = await appNameGrain.Register(appId);
        if (!success)
        {
            throw new UserFriendlyException("App name already exists.");
        }

        //appid must not be registered
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(appId));
        var res = await appGrain.Register(userId, appId, appNameInput.Name);

        var ans = new ApplyAeFinderAppNameDto() { Success = res.Success };
        _logger.LogInformation("response ApplyAeFinderAppName: {0} input={1} exists={2} added={3}", userId, JsonSerializer.Serialize(appNameInput), res.Success, res.Added);
        return ans;
    }

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper,
        IBlockScanAppService blockScanAppService, ICodeAuditor codeAuditor, IKubernetesAppManager kubernetesAppManager, IOptionsMonitor<StudioOption> studioOption)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanAppService = blockScanAppService;
        _codeAuditor = codeAuditor;
        _kubernetesAppManager = kubernetesAppManager;
        _studioOption = studioOption.CurrentValue;
    }

    public async Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(string appId, AddOrUpdateAeFinderAppInput input)
    {
        var userId = CurrentUser.GetId().ToString("N");
        if (!IsAdminOf(userId, appId))
        {
            _logger.LogError("UpdateAeFinderApp: {0} is not admin of {1}", CurrentUser.GetId(), input.AppId);
            throw new UserFriendlyException("You are not admin of this app.");
        }

        input.AppId = appId;
        _logger.LogInformation("receive request UpdateAeFinderApp: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(input));

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(appId));
        var result = await appGrain.AddOrUpdateAppInfo(_objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        var userIds = new List<string>() { userId };
        userIds.AddRange(result.DeveloperIds);

        // we do not wait for the result of AddToUsersApps
        AddToUsersApps(userIds, appId, _objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        return new AddOrUpdateAeFinderAppDto();
    }

    public async Task<AeFinderAppInfoDto> GetAeFinderApp(string clientId, GetAeFinderAppInfoInput input)
    {
        _logger.LogInformation("receive request GetAeFinderApp: adminId= {0} input= {1}", CurrentUser.GetId().ToString("N"), JsonSerializer.Serialize(input));
        AuthDeveloper(input.AppId, clientId);
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(input.AppId));
        var info = await userAppGrain.GetAppInfo();
        return info == null ? null : _objectMapper.Map<AeFinderAppInfo, AeFinderAppInfoDto>(info);
    }

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
        var apps = await _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(CurrentUser.GetId().ToString("N"))).GetApps();
        var resonse = new List<AeFinderAppInfo>();
        if (!apps.IsNullOrEmpty())
        {
            resonse.AddRange(apps.Select(app => new AeFinderAppInfo()
            {
                AppId = app.Key,
                Name = app.Value.Name,
                Description = app.Value.Description,
                LogoUrl = app.Value.LogoUrl,
                SourceCodeUrl = app.Value.SourceCodeUrl
            }));
        }

        return resonse;
    }

    public async Task<string> SubmitSubscriptionInfoAsync(string clientId, SubscriptionInfo input)
    {
        var subscriptionManifestDto = JsonSerializer.Deserialize<SubscriptionManifestDto>(input.SubscriptionManifest);
        if (subscriptionManifestDto == null || subscriptionManifestDto.SubscriptionItems.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid subscription manifest.");
        }

        AuthDeveloper(input.AppId, clientId);
        _codeAuditor.Audit(input.AppDll);
        var version = await _blockScanAppService.AddSubscriptionAsync(input.AppId, subscriptionManifestDto, input.AppDll);
        var appGraphQl = await _kubernetesAppManager.CreateNewAppPodAsync(input.AppId, version, _studioOption.ImageName);
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(input.AppId));
        await appGrain.SetGraphQlByVersion(version, appGraphQl);
        return appGraphQl;
    }

    public Task<string> QueryAeFinderAppAsync(string clientId, QueryAeFinderAppInput input)
    {
        throw new NotImplementedException();
    }

    public Task<string> QueryAeFinderAppLogsAsync(string clientId, QueryAeFinderAppLogsInput input)
    {
        throw new NotImplementedException();
    }

    private async Task AddToUsersApps(IEnumerable<string> userIds, string appId, AeFinderAppInfo info)
    {
        var tasks = userIds.Select(userId => _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId))).Select(userAppsGrain => userAppsGrain.AddApp(appId, info)).ToList();
        await tasks.WhenAll();
    }

    private bool IsAdminOf(string adminId, string appId)
    {
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(appId));
        return appGrain.IsAdmin(adminId).Result;
    }
}