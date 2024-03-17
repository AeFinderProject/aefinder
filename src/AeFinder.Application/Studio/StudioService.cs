using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using System.Text.Json;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Option;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Volo.Abp.Users;

namespace AeFinder.Studio;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class StudioService : AeFinderAppService, IStudioService, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly StudioOption _studioOption;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly ICodeAuditor _codeAuditor;
    private readonly ILogger<StudioService> _logger;

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper, IOptionsSnapshot<StudioOption> studioOption,
        IBlockScanAppService blockScanAppService, ICodeAuditor codeAuditor)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanAppService = blockScanAppService;
        _codeAuditor = codeAuditor;
        _studioOption = studioOption.Value;
    }

    private readonly IObjectMapper _objectMapper;

    public async Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(ApplyAeFinderAppNameInput appNameInput)
    {
        var userId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request ApplyAeFinderAppName: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(appNameInput));

        if (!IsAdminOf(userId, appNameInput.AppId))
        {
            _logger.LogError("ApplyAeFinderAppName: {0} is not admin of {1}", userId, appNameInput.AppId);
            throw new UserFriendlyException("You are not admin of this app.");
        }

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(appNameInput.AppId));
        var res = await appGrain.Registe(userId, appNameInput.AppId, appNameInput.Name);
        var ans = new ApplyAeFinderAppNameDto() { Success = res.Exists };
        _logger.LogInformation("response ApplyAeFinderAppName: {0} input={1} exists={2} added={3}", userId, JsonSerializer.Serialize(appNameInput), res.Exists, res.Added);
        if (!res.Added || res.Exists)
        {
            return ans;
        }

        await appGrain.AddAppName(appNameInput.Name);
        return ans;
    }

    public async Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(AddOrUpdateAeFinderAppInput input)
    {
        var userId = CurrentUser.GetId().ToString("N");
        _logger.LogInformation("receive request UpdateAeFinderApp: adminId= {0} input= {1}", userId, JsonSerializer.Serialize(input));
        if (!IsAdminOf(userId, input.AppId))
        {
            _logger.LogError("UpdateAeFinderApp: {0} is not admin of {1}", CurrentUser.GetId(), input.AppId);
            throw new UserFriendlyException("You are not admin of this app.");
        }

        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(input.AppId));
        var result = await appGrain.AddOrUpdateAppByName(_objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        var userIds = new List<string>() { userId };
        userIds.AddRange(result.DeveloperIds);

        // we do not wait for the result of AddToUsersApps
        AddToUsersApps(userIds, input.AppId, _objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        return new AddOrUpdateAeFinderAppDto();
    }

    public async Task<AeFinderAppInfoDto> GetAeFinderApp(GetAeFinderAppInfoInput input)
    {
        _logger.LogInformation("receive request GetAeFinderApp: adminId= {0} input= {1}", CurrentUser.GetId().ToString("N"), JsonSerializer.Serialize(input));
        AuthDeveloper(input.AppId);
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(input.AppId));
        var info = await userAppGrain.GetAppByName(input.Name);
        return info == null ? null : _objectMapper.Map<AeFinderAppInfo, AeFinderAppInfoDto>(info);
    }

    private async void AuthDeveloper(string appId)
    {
        var userId = CurrentUser.GetId().ToString("N");
        if (!IsAdminOf(userId, appId))
        {
            var nameGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(appId));
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

        var nameGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(input.AppId));
        var appInfo = await nameGrain.AddDeveloperToApp(input.DeveloperId);
        if (appInfo != null || appInfo.NameToApps.IsNullOrEmpty())
        {
            //we do not wait for the result of AddApps
            _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(input.DeveloperId)).AddApps(appInfo.NameToApps);
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

    public async Task<string> SubmitSubscriptionInfoAsync(SubscriptionInfo input)
    {
        var subscriptionManifestDto = JsonSerializer.Deserialize<SubscriptionManifestDto>(input.SubscriptionManifest);
        if (subscriptionManifestDto == null || subscriptionManifestDto.SubscriptionItems.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid subscription manifest.");
        }

        AuthDeveloper(input.AppId);
        _codeAuditor.Audit(input.AppDll);
        var version = await _blockScanAppService.AddSubscriptionAsync(input.AppId, subscriptionManifestDto, input.AppDll);
        return version;
    }

    public Task DeploySubscriptionInfoAsync(string appId, string version)
    {
        AuthDeveloper(appId);
        //todo deploy app dll to k8s
        return Task.CompletedTask;
        // return _blockScanAppService.DeploySubscriptionAsync(appId, version);
    }

    private async Task AddToUsersApps(List<string> userIds, string appId, AeFinderAppInfo info)
    {
        var tasks = userIds.Select(userId => _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId))).Select(userAppsGrain => userAppsGrain.AddApp(appId, info)).ToList();
        await tasks.WhenAll();
    }

    private bool IsAdminOf(string adminId, string appId)
    {
        return _studioOption.AdminOptions.Any(option => option.AdminId.Equals(adminId) && option.AppIds.Contains(appId));
    }
}