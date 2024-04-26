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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<StudioService> _logger;
    private readonly IObjectMapper _objectMapper;

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper,
        IBlockScanAppService blockScanAppService, ICodeAuditor codeAuditor)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _blockScanAppService = blockScanAppService;
        _codeAuditor = codeAuditor;
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

        var filePath = "/app/AeFinder.TokenApp.dll";
        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        string fileName = Path.GetFileName(filePath);
        IFormFile formFile = new FormFile(fileStream, 0, fileStream.Length, null, fileName);

        await using var stream = formFile.OpenReadStream();
        var dllBytes = stream.GetAllBytes();
        _codeAuditor.Audit(dllBytes);
        var userId = CurrentUser.GetId().ToString("N");
        var userAppGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderAppGrainId(userId));
        var info = await userAppGrain.GetAppInfo();
        if (info == null || info.AppId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("app not exists.");
        }

        var version = await _blockScanAppService.AddSubscriptionAsync(info.AppId, subscriptionManifest, dllBytes);
        return version;
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

    private async Task AddToUsersAppsAsync(IEnumerable<string> userIds, string appId, AeFinderAppInfo info)
    {
        var tasks = userIds.Select(userId => _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppsGrainId(userId))).Select(userAppsGrain => userAppsGrain.AddApp(appId, info)).ToList();
        await tasks.WhenAll();
    }
}